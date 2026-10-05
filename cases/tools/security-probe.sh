#!/bin/bash
# Security probe set S-1..S-8 for the OpenFOAM substrate (plan docs/plans/fluids-round2.md §4.3; Ruling 65 DR-F2-8;
# security review cycle 1 findings 9, 10, 13 applied).
# Run from repository root. Supports --check-pins for verification of pinned inputs without full solver execution.
#
# The code inside every probe is benign: #codeStream emits the constant 0.5 as endTime; codedFixedValue sets the
# lid velocity (1 0 0) that the tutorial already uses; systemCall runs /usr/bin/true; the S-6 library does not exist;
# the S-7 payloads only touch marker files under the probe's own run directory.
# Each probe is a fresh copy of the bundled icoFoam cavity tutorial with one change. Receipts go to
# runs/<ts>-security-probe/ and are copied (user name, home path and host name redacted) to
# docs/proof/spike-03/security/<ts>/. The script edits nothing else in the repository.
#
# Environments (built here, so the OpenFOAM-side refusal is observed without the product lint in front of it):
#   default : env -i + an empty HOME (the app's own etc/controlDict: allowSystemOperations 1)          (S-1)
#   p1      : env -i + product HOME holding .OpenFOAM/2512/controlDict from the bundle                  (S-2,3,4,6)
#   p2      : env -i + empty HOME + FOAM_CONTROLDICT = the bundle bytes                                 (S-5)
#   product : cases/tools/of-run.sh itself, started from a hostile parent environment                   (S-7)
#   lint    : cases/tools/foam-dict-lint.py only, no OpenFOAM process                                    (S-8)
set -uo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
repo="$(cd "$here/../.." && pwd)"
cd "$repo"

# ---- pinned inputs: the probe runs only against the reviewed launcher, lint, record tool and bundle ----
pin_bundle="f3debe8b5541fb400b0719976f591781a2faa21f96ea7ae0dccca97e4a6ef854"
pin_launcher="6c6b4e1eba497e41239aab7c0a0a3d4da3da96c5142c6d954feb410ed5c456e6"
pin_lint="3deab275778f4685fb8cd0e44277f244a5eb9faf1db3eb126b76ddfc46c4a6b7"
pin_record="6c52f3f29e4fae711390afa5c9637f43bbdf66874603374bc2d31a685903ab54"
check_pin() {
  local got; got=$(shasum -a 256 "$1" | awk '{print $1}')
  [ "$got" = "$2" ] || { echo "security-probe: ABORT: $1 sha256 $got != pinned $2" >&2; exit 2; }
}
check_pin "$here/foam-bundle/controlDict" "$pin_bundle"
check_pin "$here/of-run.sh" "$pin_launcher"
check_pin "$here/foam-dict-lint.py" "$pin_lint"
check_pin "$here/launcher-record.py" "$pin_record"
[ -e "$repo/runs/.security-stop" ] && { echo "security-probe: ABORT: runs/.security-stop exists" >&2; exit 2; }

join_lock="${CFDW_JOIN_LOCK:-${CFDW_COORD_DIR:-$(git -C "$repo" rev-parse --path-format=absolute --git-common-dir)/coord}/join.lock}"
if [ "${1:-}" = "--check-pins" ]; then
  echo "security-probe: all pinned inputs verified ($pin_bundle, $pin_launcher, $pin_lint, $pin_record); join lock: $join_lock"
  exit 0
fi
while [ -e "$join_lock" ] || ! awk -v l="$(sysctl -n vm.loadavg | awk '{print $2}')" 'BEGIN{exit !(l<=10)}'; do
  echo "security-probe: waiting (join lock or 1-min load > 10)" >&2; sleep 30
done
app="/Applications/OpenFOAM-v2512.app/Contents/Resources"
"$app/volume" -quiet mount >/dev/null 2>&1 || { echo "security-probe: cannot mount the OpenFOAM volume" >&2; exit 2; }
tutorial="/Volumes/OpenFOAM-v2512/tutorials/incompressible/icoFoam/cavity/cavity"
[ -d "$tutorial" ] || { echo "security-probe: tutorial not found: $tutorial" >&2; exit 2; }

ts=$(date -u +%Y%m%dT%H%M%SZ)
out="runs/$ts-security-probe"
mkdir -p "$out"
out_abs="$(cd "$out" && pwd)"
summary="$out/summary.txt"
user="$(id -un)"
host="$(hostname)"

new_home() {  # $1 = empty|product
  local h; h=$(mktemp -d "/tmp/cfdw-probe-home.XXXXXX")
  if [ "$1" = product ]; then mkdir -p "$h/.OpenFOAM/2512"; cp "$here/foam-bundle/controlDict" "$h/.OpenFOAM/2512/controlDict"; fi
  echo "$h"
}
# run_env <mode> <case-dir> <log> <argv...>   mode: default | p1 | p2
run_env() {
  local mode="$1" dir="$2" log="$3"; shift 3
  local h cd_val=""
  case "$mode" in
    default) h=$(new_home empty) ;;
    p1) h=$(new_home product) ;;
    p2) h=$(new_home empty); cd_val="$(cat "$here/foam-bundle/controlDict")" ;;
  esac
  local -a envs=(PATH=/usr/bin:/bin:/usr/sbin:/sbin HOME="$h" USER="$user" LOGNAME="$user" LANG=C)
  [ -n "$cd_val" ] && envs+=(FOAM_CONTROLDICT="$cd_val")
  ( cd "$dir" && exec nice -n 10 env -i "${envs[@]}" /bin/zsh -f -e "$app/etc/openfoam" -- "$@" ) > "$dir/log.$log" 2>&1
  echo "$?" > "$dir/exit.$log"
  case "$h" in /tmp/cfdw-probe-home.*) rm -rf -- "$h" ;; esac
  return 0
}
fresh() {  # $1 = probe id -> prints the new case dir (blockMesh + 2-way decomposition, under p1)
  local dir="$out/$1"
  cp -R "$tutorial" "$dir"
  rm -f "$dir/system/PDRblockMeshDict"
  printf 'FoamFile { version 2.0; format ascii; class dictionary; object decomposeParDict; }\nnumberOfSubdomains 2;\nmethod simple;\ncoeffs { n (2 1 1); }\n' > "$dir/system/decomposeParDict"
  run_env p1 "$dir" blockMesh blockMesh
  run_env p1 "$dir" decomposePar decomposePar -force
  echo "$dir"
}
banner() { grep -o "allowSystemOperations : [A-Za-z]*" "$1" | head -1 | awk '{print $3}'; }
exit_of() { cat "$1/exit.$2"; }
dyn() { find "$1" -type d -name dynamicCode | head -1 | grep -q . && echo present || echo absent; }
ranks_with() { grep -rl -- "$2" "$1/ranks" 2>/dev/null | wc -l | tr -d ' '; }
codestream_controldict() {  # $1 = case dir; $2 = extra top-level text
  sed -i '' 's/^endTime .*$/endTime #codeStream { code #{ os << 0.5; #}; };/' "$1/system/controlDict"
  [ -n "${2:-}" ] && printf '%s\n' "$2" >> "$1/system/controlDict"
  grep -q '#codeStream' "$1/system/controlDict" || { echo "probe build failed: no #codeStream in $1" >&2; exit 2; }
}
coded_bc() {  # $1 = a 0/U file: the movingWall fixedValue becomes a codedFixedValue with the same (1 0 0)
  sed -i '' 's/^\( *type *\)fixedValue;/\1codedFixedValue; name cfdwProbeBC; code #{ operator==(vector(1, 0, 0)); #};/' "$1"
  grep -q 'codedFixedValue' "$1" || { echo "probe build failed: no codedFixedValue in $1" >&2; exit 2; }
}
verdict() { echo "$1 $2 :: $3" | tee -a "$summary"; }
msg_cs="case-supplied code may have"

echo "security probe $ts launcher=$pin_launcher lint=$pin_lint record=$pin_record bundle=$pin_bundle" > "$summary"

# S-1 positive control: #codeStream under the app default executes (Allowing; dynamicCode/ created)
d=$(fresh S-1); codestream_controldict "$d"
run_env default "$d" icoFoam icoFoam
b=$(banner "$d/log.icoFoam"); e=$(exit_of "$d" icoFoam)
if [ "$b" = Allowing ] && [ "$(dyn "$d")" = present ] && [ "$e" = 0 ]; then
  verdict S-1 PASS "executes under the app default: banner=$b exit=$e dynamicCode=present"
else
  verdict S-1 FAIL "expected Allowing + dynamicCode + exit 0; got banner=$b exit=$e dynamicCode=$(dyn "$d")"
fi

# S-2 under P0+P1: (a) serial #codeStream in controlDict; (b) codedFixedValue in each processor's 0/U, mpirun -np 2.
# Pass = both refused, the refusal on BOTH ranks in (b), no dynamicCode anywhere.
d=$(fresh S-2); cp -R "$d" "$d-serial"; codestream_controldict "$d-serial"
run_env p1 "$d-serial" icoFoam icoFoam
for f in "$d"/processor*/0/U; do coded_bc "$f"; done
run_env p1 "$d" icoFoam-np2 mpirun -np 2 --output-directory "$(cd "$d" && pwd)/ranks" icoFoam -parallel
bs=$(banner "$d-serial/log.icoFoam"); es=$(exit_of "$d-serial" icoFoam); ms=$(grep -c "$msg_cs" "$d-serial/log.icoFoam")
ep=$(exit_of "$d" icoFoam-np2); rp=$(ranks_with "$d" "$msg_cs")
if [ "$bs" = Disallowing ] && [ "$es" != 0 ] && [ "$ms" -ge 1 ] && [ "$ep" != 0 ] && [ "$rp" = 2 ] && [ "$(dyn "$d")" = absent ] && [ "$(dyn "$d-serial")" = absent ]; then
  verdict S-2 PASS "refused: serial exit=$es msg=$ms banner=$bs; np2 coded BC exit=$ep ranks_with_msg=$rp/2; dynamicCode=absent"
else
  verdict S-2 FAIL "serial banner=$bs exit=$es msg=$ms dyn=$(dyn "$d-serial"); np2 exit=$ep ranks_with_msg=$rp/2 dyn=$(dyn "$d")"
fi

# S-3 systemCall function object under P0+P1: refused
d=$(fresh S-3)
printf 'functions\n{\n    sc { type systemCall; libs (utilityFunctionObjects); executeCalls (); writeCalls ("/usr/bin/true"); endCalls (); }\n}\n' >> "$d/system/controlDict"
run_env p1 "$d" icoFoam icoFoam
e=$(exit_of "$d" icoFoam); m=$(grep -c "Executing user-supplied system calls" "$d/log.icoFoam")
if [ "$e" != 0 ] && [ "$m" -ge 1 ]; then verdict S-3 PASS "refused: exit=$e msg=$m"; else verdict S-3 FAIL "exit=$e msg=$m"; fi

# S-4 case InfoSwitches { allowSystemOperations 1; } + a codedFixedValue (built after Time has read the case
# controlDict, so the case switch has had its chance) under P0+P1: still refused, no dynamicCode
d=$(fresh S-4)
printf 'InfoSwitches { allowSystemOperations 1; }\n' >> "$d/system/controlDict"
coded_bc "$d/0/U"
run_env p1 "$d" icoFoam icoFoam
e=$(exit_of "$d" icoFoam); m=$(grep -c "$msg_cs" "$d/log.icoFoam"); u=$(grep -c "unregistered" "$d/log.icoFoam")
if [ "$e" != 0 ] && [ "$m" -ge 1 ] && [ "$(dyn "$d")" = absent ]; then
  verdict S-4 PASS "still refused: exit=$e msg=$m dynamicCode=absent unregistered_lines=$u"
else
  verdict S-4 FAIL "exit=$e msg=$m unregistered_lines=$u dynamicCode=$(dyn "$d")"
fi

# S-5 as S-2 under P0+P2 (FOAM_CONTROLDICT only, empty HOME): pre-flight Disallowing; serial and np2 refused
d=$(fresh S-5)
run_env p2 "$d" checkMesh checkMesh
pb=$(banner "$d/log.checkMesh")
cp -R "$d" "$d-serial"; codestream_controldict "$d-serial"
run_env p2 "$d-serial" icoFoam icoFoam
for f in "$d"/processor*/0/U; do coded_bc "$f"; done
run_env p2 "$d" icoFoam-np2 mpirun -np 2 --output-directory "$(cd "$d" && pwd)/ranks" icoFoam -parallel
es=$(exit_of "$d-serial" icoFoam); ms=$(grep -c "$msg_cs" "$d-serial/log.icoFoam")
ep=$(exit_of "$d" icoFoam-np2); rp=$(ranks_with "$d" "$msg_cs")
if [ "$pb" = Disallowing ] && [ "$es" != 0 ] && [ "$ms" -ge 1 ] && [ "$ep" != 0 ] && [ "$rp" = 2 ] && [ "$(dyn "$d")" = absent ] && [ "$(dyn "$d-serial")" = absent ]; then
  verdict S-5 PASS "pre-flight=$pb; serial exit=$es msg=$ms; np2 coded BC exit=$ep ranks_with_msg=$rp/2; dynamicCode=absent"
else
  verdict S-5 FAIL "pre-flight=$pb serial exit=$es msg=$ms dyn=$(dyn "$d-serial"); np2 exit=$ep ranks_with_msg=$rp/2 dyn=$(dyn "$d")"
fi

# S-6 libs ("libcfdwNoSuchLib.dylib") under P0+P1: the load is attempted (documents the ungated path)
d=$(fresh S-6)
printf 'libs ("libcfdwNoSuchLib.dylib");\n' >> "$d/system/controlDict"
run_env p1 "$d" icoFoam icoFoam
m=$(grep -c "libcfdwNoSuchLib" "$d/log.icoFoam")
if [ "$m" -ge 1 ]; then verdict S-6 PASS "load attempted (ungated path documented): lines naming the library=$m"; else verdict S-6 FAIL "no line names the library"; fi

# S-7 hostile parent environment, then the product launcher. Live payloads, each leaving a marker if it runs inside
# the OpenFOAM process environment: BASH_ENV (fires only when bash runs etc/openfoam), a WM_PROJECT_SITE with
# etc/prefs.sh (sourced by etc/bashrc) and a site controlDict with allowSystemOperations 1, and an inherited
# FOAM_CONTROLDICT with allowSystemOperations 1 (not discriminating on its own: the launcher sets that variable
# itself; S-5 covers P2). Pass = pre-flight Disallowing, exit 0, no marker.
d="$out/S-7"; cp -R "$tutorial" "$d"; rm -f "$d/system/PDRblockMeshDict" "$d/system/decomposeParDict"
sed -i '' 's/^runTimeModifiable .*$/runTimeModifiable false;/' "$d/system/controlDict"
sed -i '' 's/^        \$p;$/        solver PCG; preconditioner DIC; tolerance 1e-06;/' "$d/system/fvSolution"   # no $-macro (lint rule 7)
( cd "$d" && python3 -c 'import hashlib,json,os
fs=[f"{s}/{n}" for s in ("system","constant","0") for n in sorted(os.listdir(s)) if os.path.isfile(os.path.join(s,n))]
json.dump({f: hashlib.sha256(open(f,"rb").read()).hexdigest() for f in fs}, open("cfdw-manifest.json","w"), indent=1)' )
python3 "$here/launcher-record.py" init "$d" > "$d/log.record-init" 2>&1
site="$out_abs/S-7-site"; mkdir -p "$site/etc"
printf 'touch "%s/marker.prefs-sh"\n' "$out_abs" > "$site/etc/prefs.sh"
sed 's/^\(    allowSystemOperations   \)0;/\11;/' "$here/foam-bundle/controlDict" > "$site/etc/controlDict"
printf 'case "$0" in */etc/openfoam) touch "%s/marker.bash-env" ;; esac\n' "$out_abs" > "$out_abs/S-7-bashenv.sh"
hostile="$(sed 's/^\(    allowSystemOperations   \)0;/\11;/' "$here/foam-bundle/controlDict")"
( export FOAM_CONTROLDICT="$hostile" WM_PROJECT_SITE="$site" BASH_ENV="$out_abs/S-7-bashenv.sh"
  "$here/of-run.sh" "$d" blockMesh blockMesh && "$here/of-run.sh" "$d" checkMesh-hostile checkMesh ) > "$d/launcher.out" 2>&1
e7=$?
b7=$(banner "$d/log.checkMesh-hostile")
markers=$(ls "$out_abs" | grep -c '^marker\.' || true)
if [ "$b7" = Disallowing ] && [ "$e7" = 0 ] && [ "$markers" = 0 ] && [ ! -e "$repo/runs/.security-stop" ]; then
  verdict S-7 PASS "hostile FOAM_CONTROLDICT, WM_PROJECT_SITE (prefs.sh + controlDict), BASH_ENV stripped: pre-flight=$b7 exit=$e7 markers=0"
else
  verdict S-7 FAIL "pre-flight=$b7 exit=$e7 markers=$markers ($(ls "$out_abs" | grep '^marker\.' | tr '\n' ' ')) stop_file=$([ -e "$repo/runs/.security-stop" ] && echo written || echo absent)"
fi

# S-8 lint only: #include, a regex keyword and functionObjectLibs are refused before any launch
d="$out/S-8"; mkdir -p "$d/system"
printf 'FoamFile { version 2.0; format ascii; class dictionary; object controlDict; }\n#include "x"\n"(U|p)" { relTol 0.1; }\nfunctionObjectLibs ("libforces.dylib");\n' > "$d/system/controlDict"
python3 "$here/foam-dict-lint.py" "$d" > "$d/log.lint" 2>&1
e8=$?
i8=$(grep -c "directive '#include'" "$d/log.lint"); r8=$(grep -c "quoted keyword" "$d/log.lint"); f8=$(grep -c "functionObjectLibs" "$d/log.lint")
if [ "$e8" = 3 ] && [ "$i8" -ge 1 ] && [ "$r8" -ge 1 ] && [ "$f8" -ge 1 ]; then
  verdict S-8 PASS "lint refused: exit=$e8 include=$i8 quoted_keyword=$r8 functionObjectLibs=$f8"
else
  verdict S-8 FAIL "lint exit=$e8 include=$i8 quoted_keyword=$r8 functionObjectLibs=$f8"
fi

# ---- receipts: logs, exits, summary; user name, home path and host name redacted ----
dest="docs/proof/spike-03/security/$ts"
mkdir -p "$dest"
find "$out" -type f \( -name 'log.*' -o -name 'exit.*' -o -name 'summary.txt' -o -name 'launcher.out' -o -name 'run-ledger.txt' -o -path '*/ranks/*' \) | while read -r f; do
  rel="${f#"$out"/}"; rel="${rel//prterun-$host-/prterun-HOST-}"; mkdir -p "$dest/$(dirname "$rel")"
  sed -e "s#$HOME#<HOME>#g" -e "s#/Users/$user#<HOME>#g" -e "s#$user#<USER>#g" -e "s#prterun-$host-#prterun-<HOST>-#g" -e "s#^Host *:.*#Host   : <HOST>#" "$f" > "$dest/$rel"
done
for p in S-1 S-2 S-2-serial S-3 S-4 S-5 S-5-serial S-6; do
  [ -d "$out/$p" ] && dyn "$out/$p" > "$dest/$p/dynamicCode.txt"
done
echo "receipts -> $dest"
pass=$(grep -c " PASS " "$summary"); fail=$(grep -c " FAIL " "$summary")
echo "security-probe: $pass PASS, $fail FAIL (S-1 PASS = the probe can execute under the app default)"
[ "$fail" -eq 0 ]
