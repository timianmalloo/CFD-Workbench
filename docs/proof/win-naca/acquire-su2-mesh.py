"""Acquire the publisher test mesh at the already observed immutable tree SHA."""
import hashlib,json,pathlib,urllib.request
out=pathlib.Path(__file__).resolve().parent/'sources'
tree=json.loads((out/'su2-testcases-tree.json').read_bytes())
url='https://raw.githubusercontent.com/su2code/TestCases/'+tree['sha']+'/rans/naca0012/n0012_225-65.su2'
target=out/'publisher-n0012_225-65.su2'
assert not target.exists()
with urllib.request.urlopen(url,timeout=60) as response: data=response.read()
target.write_bytes(data)
print(json.dumps(dict(url=url,bytes=len(data),sha256=hashlib.sha256(data).hexdigest(),tree_sha=tree['sha'])))
