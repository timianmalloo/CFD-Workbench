#!/bin/bash
# S6 tip BL route driver (a copy of mesh-gate-coupon.sh without the S4 surface receipts, with cellVolume written)
# S4 tip mesh coupon driver (docs/plans/tip-handling.md §4.2; Ruling 88 D5): a copy of mesh-gate-r3.sh for the coupon
# generator and locator, with the operator's machine-courtesy rule (1-minute load below 30, poll 30 s, proceed after
# 20 min and record the load) and the wing-surface receipts. Round-3 pipeline: Gmsh boundary-layer mesh ->
# gmshToFoam -> one checkMesh with sets and fields -> cell centres -> gate + locator (DR-F3-1 A). Optional y+ screen.
# Usage: mesh-gate-tipbl.sh <case.yaml> <n-procs <= 6> [screen]
# Gmsh runs as its own process (nice 10) after the same wait as of-run.sh: 1-minute load <= 10 and no join lock at
# $(git rev-parse --git-common-dir)/coord/join.lock (round 2's mesh-gate-gmsh.sh defaulted to a scratchpad path).
# Every OpenFOAM process goes through cases/tools/of-run.sh. The case writes ASCII (write_format) so the locator can
# read the sets and fields without a binary reader.
set -euo pipefail
export CFDW_MAX_LOAD=30  # of-run.sh waits while the 1-minute load is above this (default 10)
yaml="$1"; np="$2"; screen="${3:-}"
[ "$np" -le 6 ] || { echo "np must be <= 6" >&2; exit 2; }
here="$(cd "$(dirname "$0")" && pwd)"
of="$here/of-run.sh"
rec="$here/launcher-record.py"
join_lock="${CFDW_JOIN_LOCK:-$(git -C "$here" rev-parse --path-format=absolute --git-common-dir)/coord/join.lock}"
name=$(basename "$yaml" .yaml)
if [ -n "${RESUME_RUN:-}" ]; then
  # resume after an interrupted pipeline (the calling shell ended while of-run waited): the mesh and the launcher
  # record exist; continue at gmshToFoam. The case file must already name this run.
  run="$RESUME_RUN"
  grep -q "^run_dir: $run\$" "$yaml" || { echo "refusing: $yaml does not name $run" >&2; exit 2; }
  [ -f "$run/wing.msh" ] && [ ! -f "$run/log.gmshToFoam" ] || { echo "refusing: $run is not at the gmshToFoam step" >&2; exit 2; }
  echo "resume: $(date -u +%FT%TZ) at gmshToFoam" >> "$run/run-ledger.txt"
else
grep -q '^run_dir: PENDING$' "$yaml" || { echo "refusing: $yaml already names a run" >&2; exit 2; }
waited=0
export CFDW_MAX_LOAD=30
while [ "$waited" -lt 1200 ] && { [ -e "$join_lock" ] || ! awk -v l="$(sysctl -n vm.loadavg | awk '{print $2}')" 'BEGIN{exit !(l<30)}'; }; do
  echo "mesh-gate-tipbl: waiting (join lock or 1-min load >= 30) ${waited}s" >&2; sleep 30; waited=$((waited + 30))
done
load_start=$(sysctl -n vm.loadavg | awk '{print $2}')
run="runs/$(date -u +%Y%m%dT%H%M%SZ)-$name"
sed -i '' "s#^run_dir: PENDING\$#run_dir: $run#" "$yaml"
start=$(date +%s)
set +e
PYTHONPATH=/opt/homebrew/Cellar/gmsh/4.15.2/lib nice -n 10 /usr/bin/time -l -o "$run.time.gmsh" \
  uv run --quiet --with pyyaml python3 "$here/make-tip-bl-gmsh.py" "$yaml" "$run" > "$run.generator.txt" 2>&1
st=$?
set -e
mkdir -p "$run"
mv "$run.generator.txt" "$run/generator.txt"; mv "$run.time.gmsh" "$run/time.gmsh"
echo "gmsh: load1_start=$load_start status=$st wall_s=$(( $(date +%s) - start )) waited_s=$waited load1_end=$(sysctl -n vm.loadavg | awk '{print $2}')" | tee -a "$run/run-ledger.txt"
grep -E "^gmsh_wing|^layer_total|^msh_sha256|^te_arc" "$run/generator.txt" || true
[ "$st" -eq 0 ] || exit "$st"
python3 "$rec" init "$run"
fi
"$of" "$run" gmshToFoam gmshToFoam wing.msh
# product pipeline step: patch types (wing wall, symmetry, farfield patch), then re-pin
python3 -c '
import re, sys
p = sys.argv[1] + "/constant/polyMesh/boundary"
t = open(p).read()
for name, typ in (("wing", "wall"), ("symmetry", sys.argv[2]), ("farfield", "patch")):
    t, n = re.subn(r"(\n\s*" + name + r"\s*\{[^}]*?type\s+)\w+;", r"\g<1>" + typ + ";", t)
    assert n == 1, (name, n)
t = re.sub(r"\n\s*physicalType\s+\w+;", "", t)
open(p, "w").write(t)
print("patches:", re.findall(r"\n\s*(\w+)\s*\{\s*type", t))
' "$run" "$(sed -n 's/^    symmetry_patch_type: \(.*\)$/\1/p' "$yaml")"
python3 "$rec" update "$run"
"$of" "$run" checkMesh checkMesh -constant -allGeometry -allTopology -writeSets vtk \
  -writeFields '(nonOrthoAngle cellDeterminant cellShapes faceWeight cellVolume)'
"$of" "$run" postProcess-C postProcess -constant -func writeCellCentres
uv run --quiet --with pyyaml --with numpy --with scipy python3 "$here/mesh-locate-tipbl.py" "$run" "$yaml" > "$run/log.locate" 2>&1 \
  || { cat "$run/log.locate"; exit 1; }
cat "$run/log.locate"
read -r chord half <<< "$(uv run --quiet --with pyyaml python3 -c 'import sys,yaml; c=yaml.safe_load(open(sys.argv[1])); print(c["geometry"]["chord"], c["geometry"]["half_span"])' "$yaml")"
if [ "$screen" = screen ]; then
  cp -R "$run/0.orig" "$run/0"
  python3 "$rec" update "$run"
  "$of" "$run" decomposePar decomposePar -force
  "$of" "$run" simpleFoam mpirun -np "$np" simpleFoam -parallel
  "$of" "$run" reconstructPar reconstructPar -latestTime
  python3 "$here/yplus-area.py" "$run" "$chord" "$half" span-z > "$run/yplus-area.txt"
  cat "$run/yplus-area.txt"
fi
echo "run=$run"
