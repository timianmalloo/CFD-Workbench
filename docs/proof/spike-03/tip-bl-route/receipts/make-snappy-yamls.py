import pathlib
import yaml

root = pathlib.Path("/Users/mallalieut/projects/CFD-Workbench-spike-tip-bl-route-s6/cases")
base = yaml.safe_load((root / "spike03r2-m1b-ar8.yaml").read_text())
v1 = yaml.safe_load((root / "spike03-s6-w1.yaml").read_text())


def mk(name, run_id, question, desc, feat_angle, buf):
    c = yaml.safe_load(yaml.safe_dump(base))
    c["name"] = name
    c["description"] = desc
    c["purpose"] = {"spike": "SPIKE-03", "stage": 1, "round": 5, "run_id": run_id, "question": question}
    g = c["geometry"]
    for k in ("chord", "span", "half_span", "aspect_ratio", "te_thickness_m", "twist_deg", "airfoil"):
        g[k] = v1["geometry"][k]
    g["tip"] = "flat"
    g["tip_method"] = "planar cap at the half span (y = b/2 in the snappy frame: x chord, y span, z thickness); convex cap-to-skin edges of about 90 deg"
    g["te_method"] = "thicken (linear ramp), the TE base a half circle of radius 0.25 mm, as the S4 coupon"
    g["label"] = "TE blunted for analysis; tip variant for a mesh coupon only; no flow solution"
    g["source"] = {"kind": "built-planform", "path": "constant/triSurface/wing.stl", "sha256": "PENDING",
                   "generator": "cases/tools/make-tip-bl-snappy.py (copy of make-wing-r2.py), run by hand-written script per preregistration"}
    g.pop("tip_variant", None)
    c["conditions"] = v1["conditions"]
    s = c["mesh"]["settings"]
    s["layer_feature_angle_deg"] = feat_angle
    s["n_buffer_cells_no_extrude"] = buf
    s["location_in_mesh"] = [-0.6, 0.012, 0.6]
    c["mesh"]["generator"] = "snappyHexMesh"
    c["run_dir"] = "PENDING"
    c["outputs_expected"] = ["generator.txt (STL sha256, volume check)", "log.snappyHexMesh", "log.checkMesh", "log.locate (tip gate)"]
    return c


a = mk("spike03-s6-w2a", "S6-W2a", "Does snappyHexMesh addLayers (featureAngle 180, layers extruded around the 90 deg edge) pass the tip region on the V1 coupon?",
       "SPIKE-03 S6 route W2a (Ruling 97): V1 coupon, snappyHexMesh with round-2 M1b settings unchanged, layers extruded around the cap edge (featureAngle 180). Pre-registered in docs/proof/spike-03/tip-bl-route/preregistration.md before any mesh.",
       180, 0)
b = mk("spike03-s6-w2b", "S6-W2b", "Does snappyHexMesh addLayers with layer termination at the 90 deg edge (featureAngle 80, 1 buffer cell) pass the tip region on the V1 coupon?",
       "SPIKE-03 S6 route W2b (Ruling 97): V1 coupon, snappyHexMesh with round-2 M1b settings except featureAngle 80 and nBufferCellsNoExtrude 1, so layers stop at the cap edge. Pre-registered in docs/proof/spike-03/tip-bl-route/preregistration.md before any mesh.",
       80, 1)
for n, c in (("a", a), ("b", b)):
    (root / f"spike03-s6-w2{n}.yaml").write_text(yaml.safe_dump(c, sort_keys=False, width=140))
print("ok")
