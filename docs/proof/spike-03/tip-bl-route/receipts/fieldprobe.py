import gmsh
gmsh.initialize(["-nopopup"])
gmsh.model.add("p")
f = gmsh.model.mesh.field.add("BoundaryLayer")
for k in ["CurvesList","EdgesList","PointsList","SurfacesList","FanPointsList","FanNodesList","FanPointsSizesList","IntersectMetrics","Quads","Ratio","Size","Thickness","NbLayers","SizeFar","ExcludedSurfacesList","AnisoMax","hwall_n","hfar","BetaLaw"]:
    try:
        gmsh.model.mesh.field.setNumbers(f,k,[1]); print("ok list",k)
    except Exception as e:
        try:
            gmsh.model.mesh.field.setNumber(f,k,1); print("ok num",k)
        except Exception as e2: print("NO",k)
gmsh.finalize()
