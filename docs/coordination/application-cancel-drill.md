---
id: coordination-application-cancel-drill
title: First-core built-in worker cancellation drill
type: proof-pack
status: reviewed
owner: "@cfd-coordinator-20260923"
tags: [coordination, application, cancellation]
links:
  - {to: coordination-application-build, rel: relates-to}
  - {to: coordination-contract-b-core, rel: relates-to}
review-by: 2026-10-23
summary: Observed built-in agent interruption and explicit owned-child cleanup; automatic subprocess cancellation is not established.
review-suggested:
  - { by: coordination-application-build, on: 2026-09-23, reason: "Active-seat dispatch control and observed serial core checkpoints added; review execution references." }
---

# First-core cancellation drill

The disposable built-in collaborator was requested as `gpt-6-astra`; its
effective model identifier was **Not recorded** by the collaborator interface.
The assigned worktree was
`/Users/mallalieut/projects/CFD-Workbench-feature-application-cancel-drill`,
branch `feature/application-cancel-drill`, session
`cfd-cancel-drill-20260923`, base
`c13db2737440d78d384f48afa018af268e70f807`. The worker read back that
exact cwd, branch and HEAD, with empty `git status --short`. Its audit start
marker returned `2026-09-23T15:10:41Z`. It authored no product files.

The worker launched a harmless active child with
`python3 -c 'import os,time; print(os.getpid(),flush=True); time.sleep(300)'`
through its `exec_command` terminal session `45555`, and sent PID `70844` to
the Coordinator. The Coordinator called `interrupt_agent`; the tool returned
`previous_status: running`. A subsequent collaborator inventory reported the
worker as `interrupted`. The tool transcript's immediate `ps` readback was:

```text
70844 21474 Ss+  /opt/homebrew/Cellar/python@3.14/3.14.4_1/Frameworks/Python.framework/Versions/3.14/Resources/Python.app/Contents/MacOS/Python -c import os,time; print(os.getpid(),flush=True); time.sleep(300)
```

Thus agent interruption **did not** stop its owned subprocess. A Coordinator
attempt to attach to the worker's terminal session with `write_stdin` returned
`Unknown process id 45555`; cross-agent terminal ownership is unavailable.
The Coordinator sent `kill -TERM 70844` to the exact observed owned PID. A
subsequent `ps -p 70844 -o pid=,ppid=,stat=,command=` returned exit code 1
with empty output. The disposable worktree remained clean. These observations
are transcribed here from the tool-call outputs; the harness did not retain a
separate raw stdout file. No automatic child cleanup or enforced filesystem
confinement is claimed.

Under Owner Ruling 11's interpretation at this gate, this is sufficient
observed agent cancellation **and explicit owned-child quiescence**. The first
core monitor must capture PID plus process start identity before interruption,
stop dispatch, terminate only verified owned children, read back their
absence, and stop for review on identity drift or live unmanaged processes.
This drill does not clear the separate G3 technical and path/worker preflight.
