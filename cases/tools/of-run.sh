#!/bin/bash -p
# Product launcher (spike form) for one OpenFOAM process in the OpenFOAM-v2512.app environment.
# Usage: of-run.sh <case-dir> <log-name> <app> [args...]
#        of-run.sh <case-dir> <log-name> mpirun -np <N <= 6> <app> [args...]
# argv form only; no shell string is ever built. <app> must be on APPS below.
#
# Security (plan docs/plans/fluids-round2.md §4.2, Ruling 65; security review cycle 1 applied):
#   P0  the process starts from `env -i` with an allow-listed environment only (PATH, HOME, USER, LOGNAME, LANG,
#       FOAM_CONTROLDICT); no inherited FOAM_*, WM_*, BASH_ENV, DYLD_*, OMPI_*.
#   P1  HOME is a fresh product-owned directory holding .OpenFOAM/2512/controlDict written from the bundle.
#   P2  FOAM_CONTROLDICT carries the same bundle bytes (allowSystemOperations 0), so no controlDict file is merged.
#       The bundle's sha256 is pinned below and re-verified at every launch, and again on the written copy.
#   Right before the launch (after any resource wait):
#     - cases/tools/launcher-record.py verify: the case tree equals the tree recorded outside the case at generation
#       and after every earlier launch (no new, changed or missing file; no shared library or executable image);
#     - cases/tools/foam-dict-lint.py: token-level allow-list lint of every generator-written dictionary;
#     - every app except the mesh writers (blockMesh, gmshToFoam) and checkMesh needs the checkMesh pre-flight record ("Disallowing" seen under
#       this bundle for this case and manifest), kept in the launcher record.
#   After the launch: "Allowing" in the log writes the fixed stop file runs/.security-stop and every later launch
#   refuses; a missing banner fails the launch; the tree is re-pinned (OpenFOAM's outputs become recorded).
#   Launch: /bin/zsh -f <app launcher> -- <argv...> -> etc/openfoam `exec "$@"` (never the -c / bash -c form). The
#   case directory may not hold a file named like the first argv word (etc/openfoam runs such a file with bash).
# Resources (Ruling 60): nice -n 10; wait while the 1-minute load is above 10 or the join lock exists.
# Peak RSS: /usr/bin/time -l writes <case>/time.<log-name>; the ledger records "maximum resident set size".
set -euo pipefail
[ "$#" -ge 3 ] || { echo "usage: of-run.sh <case-dir> <log-name> <app> [args...]" >&2; exit 2; }
case_dir="$1"; log_name="$2"; shift 2
here="$(cd "$(dirname "$0")" && pwd)"
repo="$(cd "$here/../.." && pwd)"
bundle="$here/foam-bundle/controlDict"
bundle_sha256_pinned="f3debe8b5541fb400b0719976f591781a2faa21f96ea7ae0dccca97e4a6ef854"
app_launcher="/Applications/OpenFOAM-v2512.app/Contents/Resources/etc/openfoam"
stop_file="$repo/runs/.security-stop"
max_load=10
join_lock="${CFDW_JOIN_LOCK:-/private/tmp/claude-501/-Users-mallalieut-projects-CFD-Workbench/f19a2b12-f8df-4dcc-bc84-7353cfbcda0f/scratchpad/join.lock}"
APPS=" checkMesh blockMesh gmshToFoam decomposePar reconstructPar reconstructParMesh snappyHexMesh surfaceCheck surfaceFeatureExtract simpleFoam rhoSimpleFoam postProcess topoSet "
ledger="$case_dir/run-ledger.txt"
now() { date -u +%Y-%m-%dT%H:%M:%SZ; }
refuse() { echo "of-run: REFUSED: $1" >&2; echo "of-run: refused $(now) $1 cmd=${*:2}" >> "$ledger"; exit "${2:-90}"; }

[ -d "$case_dir" ] || { echo "of-run: no case directory $case_dir" >&2; exit 2; }
[ -e "$stop_file" ] && refuse "security stop file present ($stop_file): an 'Allowing' banner was seen" 98
bundle_sha=$(shasum -a 256 "$bundle" | awk '{print $1}')
[ "$bundle_sha" = "$bundle_sha256_pinned" ] || refuse "bundle sha256 $bundle_sha != pinned $bundle_sha256_pinned" 97

# argv shape: <app> ... or mpirun -np <N> <app> ...; the app is checked against APPS, never inferred.
if [ "$1" = mpirun ]; then
  [ "$#" -ge 4 ] && [ "$2" = "-np" ] && [[ "$3" =~ ^[1-6]$ ]] || refuse "mpirun form must be: mpirun -np <1-6> <app> ..." 93
  app="$4"
else
  app="$1"
fi
case "$app" in */*|"") refuse "app name '$app' must be a bare name" 93 ;; esac
[[ "$APPS" == *" $app "* ]] || refuse "app '$app' is not on the launcher allow-list" 93
[ -e "$case_dir/$1" ] && refuse "the case holds a file named '$1' (etc/openfoam would run it with bash)" 93
[ -e "$case_dir/$app" ] && refuse "the case holds a file named '$app'" 93
case "$app" in blockMesh|gmshToFoam|checkMesh) needs_preflight=no ;; *) needs_preflight=yes ;; esac

waited=0
while :; do
  load=$(sysctl -n vm.loadavg | awk '{print $2}')
  if [ ! -e "$join_lock" ] && awk -v l="$load" -v m="$max_load" 'BEGIN{exit !(l<=m)}'; then break; fi
  if [ -e "$join_lock" ]; then reason="join lock present"; else reason="load $load > $max_load"; fi
  echo "of-run: $reason; waiting (${waited}s)" >&2
  echo "of-run: wait $(now) $reason" >> "$ledger"
  sleep 30; waited=$((waited + 30))
  if [ "$waited" -ge 3600 ]; then echo "of-run: not clear after 60 min" >&2; exit 75; fi
done

# Checks run here, right before the launch (no resource wait between them and the process start).
if ! rec_out=$(python3 "$here/launcher-record.py" verify "$case_dir" 2>&1); then
  printf '%s\n' "$rec_out" > "$case_dir/log.record-$log_name"
  refuse "case tree differs from the launcher record (see log.record-$log_name)" 96
fi
if ! lint_out=$(python3 "$here/foam-dict-lint.py" "$case_dir" 2>&1); then
  printf '%s\n' "$lint_out" > "$case_dir/log.lint-$log_name"
  refuse "dictionary lint (see log.lint-$log_name)" 96
fi
if [ "$needs_preflight" = yes ]; then
  python3 "$here/launcher-record.py" preflight-check "$case_dir" >/dev/null || refuse "no checkMesh pre-flight with the Disallowing banner for this case" 95
fi

home=$(mktemp -d "/tmp/cfdw-home.XXXXXX")
trap 'case "$home" in /tmp/cfdw-home.*) rm -rf -- "$home" ;; esac' EXIT
mkdir -p "$home/.OpenFOAM/2512"
cp "$bundle" "$home/.OpenFOAM/2512/controlDict"
[ "$(shasum -a 256 "$home/.OpenFOAM/2512/controlDict" | awk '{print $1}')" = "$bundle_sha256_pinned" ] \
  || refuse "written HOME controlDict hash mismatch" 97
foam_controldict="$(cat "$bundle")"

echo "of-run: start $(now) load1=$load record=verified lint=clean bundle=${bundle_sha256_pinned:0:12} cmd=$*" >> "$ledger"
start=$(date +%s)
set +e
( cd "$case_dir" && exec nice -n 10 /usr/bin/time -l -o "time.$log_name" \
    env -i PATH=/usr/bin:/bin:/usr/sbin:/sbin HOME="$home" USER="$(id -un)" LOGNAME="$(id -un)" LANG=C \
    FOAM_CONTROLDICT="$foam_controldict" \
    /bin/zsh -f -e "$app_launcher" -- "$@" ) > "$case_dir/log.$log_name" 2>&1
status=$?
set -e
end=$(date +%s)
load_end=$(sysctl -n vm.loadavg | awk '{print $2}')
rss=$(awk '/maximum resident set size/{print $1}' "$case_dir/time.$log_name" 2>/dev/null || true)
if grep -q "allowSystemOperations : Allowing" "$case_dir/log.$log_name"; then
  banner=Allowing
elif grep -q "allowSystemOperations : Disallowing" "$case_dir/log.$log_name"; then
  banner=Disallowing
else
  banner=absent
fi
python3 "$here/launcher-record.py" update "$case_dir" > /dev/null || { echo "of-run: re-pin refused after the launch" >&2; status=92; }
echo "of-run: end status=$status wall_s=$((end - start)) load1=$load_end peak_rss_bytes=${rss:-not-recorded} banner=$banner cmd=$*" >> "$ledger"
if [ "$banner" = Allowing ]; then
  mkdir -p "$(dirname "$stop_file")"
  echo "$(now) Allowing banner in $case_dir/log.$log_name" > "$stop_file"
  echo "of-run: SECURITY STOP: 'Allowing' banner under the product launcher; all runs stop" >&2; exit 99
fi
if [ "$banner" = absent ]; then
  echo "of-run: FAILED: no allowSystemOperations banner in log.$log_name (fail closed)" >&2
  [ "$status" -ne 0 ] || status=94
fi
if [ "$app" = checkMesh ] && [ "$banner" = Disallowing ] && [ "$status" -eq 0 ]; then
  python3 "$here/launcher-record.py" preflight-set "$case_dir"
fi
echo "status=$status wall_s=$((end - start)) peak_rss_bytes=${rss:-not-recorded} banner=$banner log=$case_dir/log.$log_name"
exit $status
