#!/usr/bin/env bash
# Ruling 153: availability-only OpenFOAM/cfMesh probe. No mesh or solver run.
set +e

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
evidence_dir="$script_dir/r153"
mkdir -p "$evidence_dir"
mkdir_status=$?
printf 'stage.evidence_dir_create_exit=%s\n' "$mkdir_status"

printf 'started_utc='
date -u '+%Y-%m-%dT%H:%M:%SZ'
date_status=$?
printf 'stage.start_time_exit=%s\n' "$date_status"

sha256sum -- "${BASH_SOURCE[0]}" > "$evidence_dir/script-sha256.txt"
script_hash_status=$?
printf 'stage.script_sha256_exit=%s\n' "$script_hash_status"
cat "$evidence_dir/script-sha256.txt"

cat /proc/loadavg > "$evidence_dir/loadavg-before.txt"
load_before_status=$?
printf 'stage.loadavg_before_exit=%s\n' "$load_before_status"

source /usr/lib/openfoam/openfoam2512/etc/bashrc
source_status=$?
set +e
set -u
printf 'stage.bashrc_source_exit=%s\n' "$source_status"

printf 'WM_PROJECT_VERSION=%s\n' "${WM_PROJECT_VERSION-}" > "$evidence_dir/environment.txt"
version_status=$?
printf 'WM_PROJECT_DIR=%s\n' "${WM_PROJECT_DIR-}" >> "$evidence_dir/environment.txt"
project_dir_status=$?
printf 'FOAM_APPBIN=%s\n' "${FOAM_APPBIN-}" >> "$evidence_dir/environment.txt"
appbin_status=$?
printf 'stage.environment_version_exit=%s\n' "$version_status"
printf 'stage.environment_project_dir_exit=%s\n' "$project_dir_status"
printf 'stage.environment_appbin_exit=%s\n' "$appbin_status"
cat "$evidence_dir/environment.txt"

dpkg-query -W -f='${Package} ${Version} ${Status}\n' openfoam2512 > "$evidence_dir/dpkg-query.txt" 2> "$evidence_dir/dpkg-query.stderr.txt"
dpkg_status=$?
printf 'stage.dpkg_query_exit=%s\n' "$dpkg_status"
cat "$evidence_dir/dpkg-query.txt"
cat "$evidence_dir/dpkg-query.stderr.txt"

command -v cartesianMesh > "$evidence_dir/resolved-path.txt" 2> "$evidence_dir/command-v.stderr.txt"
command_status=$?
printf 'stage.command_v_exit=%s\n' "$command_status"
cat "$evidence_dir/resolved-path.txt"
cat "$evidence_dir/command-v.stderr.txt"

resolved_path="$(cat "$evidence_dir/resolved-path.txt")"
if [ -n "$resolved_path" ]; then
  ls -l -- "$resolved_path" > "$evidence_dir/resolved-ls.txt" 2>&1
  ls_status=$?
  sha256sum -- "$resolved_path" > "$evidence_dir/resolved-sha256.txt" 2>&1
  binary_hash_status=$?
else
  ls_status=125
  binary_hash_status=125
  : > "$evidence_dir/resolved-ls.txt"
  : > "$evidence_dir/resolved-sha256.txt"
fi
printf 'stage.resolved_ls_exit=%s\n' "$ls_status"
cat "$evidence_dir/resolved-ls.txt"
printf 'stage.resolved_sha256_exit=%s\n' "$binary_hash_status"
cat "$evidence_dir/resolved-sha256.txt"

if [ -n "$resolved_path" ]; then
  "$resolved_path" -help > "$evidence_dir/cartesianMesh-help.stdout.txt" 2> "$evidence_dir/cartesianMesh-help.stderr.txt"
  help_status=$?
else
  help_status=125
  : > "$evidence_dir/cartesianMesh-help.stdout.txt"
  : > "$evidence_dir/cartesianMesh-help.stderr.txt"
fi
printf 'stage.cartesianMesh_help_exit=%s\n' "$help_status"

if [ -n "${FOAM_APPBIN-}" ] && [ -e "$FOAM_APPBIN/cartesianMesh" ]; then
  appbin_binary_status=0
else
  appbin_binary_status=1
fi
printf 'stage.foam_appbin_binary_check_exit=%s\n' "$appbin_binary_status"
printf 'FOAM_APPBIN_cartesianMesh_exists=%s\n' "$appbin_binary_status"

cat /proc/loadavg > "$evidence_dir/loadavg-after.txt"
load_after_status=$?
printf 'stage.loadavg_after_exit=%s\n' "$load_after_status"
cat "$evidence_dir/loadavg-before.txt"
cat "$evidence_dir/loadavg-after.txt"

printf 'ended_utc='
date -u '+%Y-%m-%dT%H:%M:%SZ'
end_date_status=$?
printf 'stage.end_time_exit=%s\n' "$end_date_status"

if [ "$source_status" -ne 0 ] || [ -z "${WM_PROJECT_VERSION-}" ]; then
  outcome='environment unresolved'
elif [ -n "$resolved_path" ] && [ "$help_status" -eq 0 ] && [ "$dpkg_status" -eq 0 ]; then
  outcome='present'
elif [ -z "$resolved_path" ] && [ "$appbin_binary_status" -ne 0 ]; then
  outcome='absent under Ruling 102'
else
  outcome='environment unresolved'
fi
printf 'outcome=%s\n' "$outcome"

