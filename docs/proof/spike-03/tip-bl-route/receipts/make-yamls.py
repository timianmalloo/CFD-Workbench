import re, pathlib
root = pathlib.Path("/Users/mallalieut/projects/CFD-Workbench-spike-tip-bl-route-s6/cases")
src = (root / "spike03-s4-tipcoupon-v1.yaml").read_text()
src = re.sub(r"run_dir: .*", "run_dir: PENDING", src)
src = re.sub(r"    sha256: [0-9a-f]{64}\n", "    sha256: PENDING\n", src)
src = src.replace("generator: cases/tools/mesh-gate-coupon.sh cases/spike03-s4-tipcoupon-v1.yaml 6 (make-tip-coupon-gmsh.py,",
                  "generator: cases/tools/mesh-gate-tipbl.sh CASE 6 (make-tip-bl-gmsh.py,")
src = re.sub(r"  deviation: >-\n(?:      .*\n)+", "    deviation: >-\n      A 25 mm half-span coupon (not the AR 8 wing); the msh sha256 is filled in after the run.\n", src) if False else src


def mk(name, run_id, tip_variant, question, desc, extra_settings, geom_extra=""):
    t = src.replace("spike03-s4-tipcoupon-v1", name)
    t = re.sub(r"description: >-\n(?:  .*\n)+", "description: >-\n  " + desc + "\n", t, count=1)
    t = t.replace("run_id: S4-V1", f"run_id: {run_id}")
    t = re.sub(r"  question: .*", f"  question: {question}", t)
    t = t.replace("  round: 4", "  round: 5")
    t = t.replace("tip_variant: v1", f"tip_variant: {tip_variant}")
    t = t.replace("  stage: 1\n", "  stage: 1\n")
    t = t.replace("    te_refine:", extra_settings + "    te_refine:")
    return t


w1 = mk("spike03-s6-w1", "S6-W1", "v1", "Does the Gmsh boundary-layer fan option change the V1 mesh in 3-D?",
        "SPIKE-03 S6 route W1 (Ruling 97): V1 planar flat cut with Mesh.BoundaryLayerFanElements = 8 set (the corner-fan option, documented for 2-D). Pre-registered in docs/proof/spike-03/tip-bl-route/preregistration.md before any mesh. Every other setting is S4 V1.",
        "    gmsh_options: { Mesh.BoundaryLayerFanElements: 8 }\n")
w4 = mk("spike03-s6-w4", "S6-W4", "w4", "Does an in-mesh perimeter fillet of radius 8 um on the V1 cap edge pass the tip region? (geometry change; operator ruling needed before adoption)",
        "SPIKE-03 S6 route W4 (Ruling 97): V1 planar flat cut with an in-mesh edge fillet of 8 um (the first cell height) on the cap perimeter, 4 segments across the fillet. A geometry change, NOT the tip of record: operator ruling needed before adoption. Pre-registered in docs/proof/spike-03/tip-bl-route/preregistration.md before any mesh. Every other setting is S4 V1.",
        "    tip_fillet_radius_m: 8.0e-6\n    tip_fillet_segments: 4\n")
(root / "spike03-s6-w1.yaml").write_text(w1)
(root / "spike03-s6-w4.yaml").write_text(w4)
print("ok")
