"""Preserve the pinned primary source contracts consulted for SU2 boundary conditions."""
import hashlib,json,pathlib,urllib.request
out=pathlib.Path(__file__).resolve().parent/'sources'
urls={'CTurbSASolver-v8.5.0.cpp':'https://raw.githubusercontent.com/su2code/SU2/v8.5.0/SU2_CFD/src/solvers/CTurbSASolver.cpp','CConfig-v8.5.0.cpp':'https://raw.githubusercontent.com/su2code/SU2/v8.5.0/Common/src/CConfig.cpp'}
for name,url in urls.items():
    path=out/name; assert not path.exists()
    with urllib.request.urlopen(url,timeout=60) as response: data=response.read()
    path.write_bytes(data)
    print(json.dumps(dict(path=name,url=url,bytes=len(data),sha256=hashlib.sha256(data).hexdigest())))
