#!/usr/bin/env bash
# Measures which file types and folders the Impeccable detector scans, for ui-craft-gate.py's SCANNED and WALK_SKIPS.
# Usage: bash docs/proof/craft-gate-scanned-types/probe.sh <impeccable executable>   (writes only under a temp dir)
# No -e or pipefail: the detector exits 2 when it finds anti-patterns, which is the result being measured (E14).
set -u
X="$1"
T=$(mktemp -d)
trap 'rm -rf "$T"' EXIT
CSS='.t{font-family:Inter,sans-serif;background:linear-gradient(90deg,#7c3aed,#ec4899);-webkit-background-clip:text;background-clip:text;color:transparent;transition:width 300ms cubic-bezier(0.68,-0.55,0.265,1.55)}'
count() { "$X" detect --json --no-design-system "$1" 2>/dev/null | python -c "import sys,json; s=sys.stdin.read(); print(len(json.loads(s[s.find('['):])))"; }
"$X" --version
# 1. A tree walk: the same CSS text under every candidate extension; the extensions reported are the ones walked.
mkdir "$T/walk"
for e in html htm xhtml css scss sass less js mjs jsx ts tsx vue svelte astro axaml xaml cshtml razor md txt; do
  printf '%s\n' "$CSS" > "$T/walk/f.$e"
done
printf '<html><head><style>%s</style></head><body><h1 class="t">Hi</h1></body></html>\n' "$CSS" > "$T/walk/page.html"
cp "$T/walk/page.html" "$T/walk/page.htm"
"$X" detect --json --no-design-system "$T/walk" 2>/dev/null \
  | python -c "import sys,json,os; s=sys.stdin.read(); print('walked:', sorted({os.path.splitext(x['file'])[1] for x in json.loads(s[s.find('['):])}))"
# 2. Avalonia XAML in its own syntax, named directly: no rule matches.
printf '<Window xmlns="https://github.com/avaloniaui"><TextBlock FontFamily="Comic Sans MS" FontSize="9" Foreground="#ff00ff">Hi</TextBlock></Window>\n' > "$T/bad.axaml"
echo "named .axaml (XAML syntax) findings: $(count "$T/bad.axaml")"
# 3. Folders the walk skips: one CSS file inside each. Parents are numbered, not named after the folder: on a
#    case-insensitive drive skip-Node_Modules and skip-node_modules would be one folder.
i=0
for d in node_modules dist build __pycache__ .hidden Node_Modules vendor out bin obj public; do
  i=$((i + 1)); mkdir -p "$T/case$i/$d"; printf '%s\n' "$CSS" > "$T/case$i/$d/a.css"
  echo "folder $d findings: $(count "$T/case$i")"
done
