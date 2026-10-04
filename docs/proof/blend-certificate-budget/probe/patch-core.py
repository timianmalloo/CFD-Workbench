#!/usr/bin/env python3
"""Make a disposable, instrumented copy of src/CfdWorkbench.Core for the blend-budget probe.

Usage: python3 patch-core.py <repo-root> <out-dir>

The copy differs from the as-built Core in four places, each a hook the probe drives. At the hooks' defaults
(node rule = 256, operation bound = 1e6) the copy computes exactly what the as-built Core computes; the probe proves
that by running the constant rule against both builds and comparing work and status (output/neutrality.md).
Nothing in src/ is changed.
"""
import pathlib
import shutil
import sys

repo, out = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
src = repo / "src" / "CfdWorkbench.Core"
if out.exists():
    shutil.rmtree(out)
shutil.copytree(src, out, ignore=shutil.ignore_patterns("bin", "obj"))
geometry = out / "Geometry.cs"
text = geometry.read_text()

PATCHES = [
    # 1. The node budget becomes a rule of the span count (default: the as-built constant).
    ("            const int blendNodes = 256;\n",
     "            int blendNodes = ProbeHooks.BlendNodes(spanCount); ProbeHooks.SpanCount = spanCount;\n"),
    # 2. The all-query operation bound becomes a parameter; the computed count is recorded before the check.
    ("        Geometry.Require(operations <= 1000000, \"All-query operation bound exceeds one million.\"",
     "        ProbeHooks.Operations = operations;\n"
     "        Geometry.Require(operations <= ProbeHooks.OperationBound, \"All-query operation bound exceeds one million.\""),
    # 3. Query time: record the nodes the two blend-maximum enclosures used and the enclosure's relative gap.
    ("        return new(low.Lower, high.Upper);\n    }\n\n    private static Rational[][] ScaleSum",
     "        ProbeHooks.ObserveQuery(low.Nodes, high.Nodes, low.Lower, high.Upper);\n"
     "        return new(low.Lower, high.Upper);\n    }\n\n    private static Rational[][] ScaleSum"),
]
for old, new in PATCHES:
    if text.count(old) != 1:
        sys.exit("patch anchor not found exactly once: " + old[:70])
    text = text.replace(old, new)
geometry.write_text(text)

# 4. The hook class (comparisons only; it constructs no Rational, so it adds no bit-work).
(out / "ProbeHooks.cs").write_text('''namespace CfdWorkbench.Core;

internal static class ProbeHooks
{
    internal static Func<int, int> Rule = _ => 256;
    internal static int BlendNodes(int spanCount) => Rule(spanCount);
    internal static long OperationBound = 1000000;
    internal static long Operations;
    internal static int SpanCount;
    internal static int QueryNodesMax;
    // The enclosure is kept, not reduced here: a Rational subtraction would charge bit-work to the query being measured.
    internal static Rational LastLower, LastUpper;
    internal static void ObserveQuery(int lowNodes, int highNodes, Rational lower, Rational upper)
    {
        QueryNodesMax = Math.Max(QueryNodesMax, Math.Max(lowNodes, highNodes));
        LastLower = lower; LastUpper = upper;
    }
    internal static void ResetQuery() { QueryNodesMax = 0; }
}
''')
print("patched copy at", out)
