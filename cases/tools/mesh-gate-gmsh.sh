#!/bin/bash
# Round-2 SPIKE-03 probe M-2 (DR-F2-5): Gmsh boundary-layer mesh -> gmshToFoam -> gate, unattended. Optional y+ screen.
# Usage: mesh-gate-gmsh.sh <case.yaml> <n-procs ≤ 6> [screen]
# Gmsh runs as its own process (nice 10, after the same load / join-lock wait as of-run.sh); every OpenFOAM process
# goes through cases/tools/of-run.sh. Patch types are set by this pipeline after gmshToFoam (gmshToFoam writes every
# patch as `patch`), then the launcher record is re-pinned.
set -euo pipefail
yaml="$1"; np="$2"; screen="${3:-}"
[ "$np" -le 6 ] || { echo "np must be <= 6" >&2; exit 2; }
here="$(cd "$(dirname "$0")" && pwd)"
of="$here/of-run.sh"
rec="$here/launcher-record.py"
join_lock="${CFDW_JOIN_LOCK:-$(git -C "$(dirname "$0")" rev-parse --path-format=absolute --git-common-dir)/coord/join.lock}"
name=$(basename "$yaml" .yaml)
grep -q '^run_dir: PENDING$' "$yaml" || { echo "refusing: $yaml already names a run" >&2; exit 2; }
run="runs/$(date -u +%Y%m%dT%H%M%SZ)-$name"
sed -i '' "s#^run_dir: PENDING\$#run_dir: $run#" "$yaml"
while [ -e "$join_lock" ] || ! awk -v l="$(sysctl -n vm.loadavg | awk '{print $2}')" 'BEGIN{exit !(l<=10)}'; do
  echo "mesh-gate-gmsh: waiting (join lock or 1-min load > 10)" >&2; sleep 30
done
start=$(date +%s)
set +e
PYTHONPATH=/opt/homebrew/Cellar/gmsh/4.15.2/lib nice -n 10 /usr/bin/time -l -o "$run.time.gmsh" \
  uv run --quiet --with pyyaml python3 "$here/make-wing-gmsh.py" "$yaml" "$run" > "$run.generator.txt" 2>&1
st=$?
set -e
mkdir -p "$run"
mv "$run.generator.txt" "$run/generator.txt"; mv "$run.time.gmsh" "$run/time.gmsh"
echo "gmsh: status=$st wall_s=$(( $(date +%s) - start ))" | tee -a "$run/run-ledger.txt"
grep -E "^gmsh_wing|^layer_total|^msh_sha256" "$run/generator.txt"
[ "$st" -eq 0 ] || exit "$st"
python3 "$rec" init "$run"
"$of" "$run" gmshToFoam gmshToFoam wing.msh
# product pipeline step: patch types (wing wall, symmetry symmetryPlane, farfield patch), then re-pin
python3 -c '
import re, sys
p = sys.argv[1] + "/constant/polyMesh/boundary"
t = open(p).read()
for name, typ in (("wing", "wall"), ("symmetry", sys.argv[2]), ("farfield", "patch")):
    t, n = re.subn(r"(\n\s*" + name + r"\s*\{[^}]*?type\s+)\w+;", r"\g<1>" + typ + ";", t)
    assert n == 1, (name, n)
t = re.sub(r"\n\s*physicalType\s+\w+;", "", t)
open(p, "w").write(t)
names = re.findall(r"\n\s*(\w+)\s*\{\s*type", t)
print("patches:", names)
' "$run" "$(sed -n 's/^    symmetry_patch_type: \(.*\)$/\1/p' "$yaml")"
python3 "$rec" update "$run"
"$of" "$run" checkMesh checkMesh -allGeometry -allTopology
"$of" "$run" checkMesh-det checkMesh -constant -allGeometry -writeFields '(cellDeterminant)'
"$of" "$run" topoSet-det topoSet -constant -dict system/topoSetDict.det
grep -E "detBelow|size" "$run/log.topoSet-det" | tail -4 || true
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
