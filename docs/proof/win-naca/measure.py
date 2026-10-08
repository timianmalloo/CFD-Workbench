"""Record source qualification and bounded solver commands without losing raw output."""
import datetime,json,pathlib,subprocess,sys,time
root=pathlib.Path(__file__).resolve().parents[3]
out=pathlib.Path(__file__).resolve().parent
tag=sys.argv[1]
argv=sys.argv[2:]
dest=out/(tag+'.txt')
assert not dest.exists(),str(dest)
started=datetime.datetime.now(datetime.timezone.utc).isoformat()
clock=time.perf_counter()
with dest.open('wb') as stream:
    result=subprocess.run(argv,cwd=root,stdout=stream,stderr=subprocess.STDOUT)
row=dict(tag=tag,argv=argv,started_utc=started,elapsed_seconds=time.perf_counter()-clock,exit_code=result.returncode,raw_output=dest.relative_to(root).as_posix())
with (out/'commands.jsonl').open('ab') as stream: stream.write((json.dumps(row)+'\n').encode())
print(json.dumps(row))
print(dest.read_bytes().decode('utf-8',errors='replace')[-10000:])
sys.exit(result.returncode)
