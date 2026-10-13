---
id: proof-win-gpu-f1-r228-attempt1-failure
title: "R228 F1 attempt 1 failure receipt"
type: proof-pack
status: blocked
owner: "@pc-f1-r225-attempt1"
phase: preflight
tags: [windows, wsl, preflight, failure-receipt]
links:
  - { to: proof-win-gpu-f1-r225-attempt1-admission, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2026-10-13"
summary: >-
  Attempt 1 stopped after a guard-event sharing failure; checkpoint binding failed,
  the fresh target remains registered, and exact cleanup requires a Mac ruling.
---

# R228 attempt 1 — failed, stopped, no rerun

The one authorized invocation exited nonzero after **39.334 seconds**.
`probe` and `repositories` completed. Reading `completed-guard-events.jsonl` failed
because another process held the file without compatible sharing. Finalization stopped
owned work and the guard, captured the protected after-snapshot, and bound the guard.

The checkpoint commit is `7c8a7984fcb101a4f29c212f221488aa1a44e637`, but
manifest verification rejected `checkpoint/invocation-contract.stdout.txt` because the
committed blob differed from the evidence. The cleanup contract therefore withheld
archive deletion and unregister. It did not guess that cleanup was safe.

The created target remains registered as `cfdw-petsc-cuda132-f1` with key
`{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}` at its dedicated target path. The protected distro retains its
exact reviewed name, GUID, version, and path in `coordinator/host-registrations-after-failure.json`.
No second invocation is authorized.

Both R228 lease samples exceeded 200 seconds: activation `874.305`
seconds and immediate pre-WSL `851.647` seconds. The committed
coordinator evidence contains the whole-run renewal events, empty supervisor stderr,
and supervisor exit 0.

**Mac decision required:** authorize or refuse one exact cleanup-only action for created
target key `{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}`. Cleanup must preserve the checkpoint and receipt,
recheck the exact target and protected identities, terminate/unregister only that target,
prove name/GUID/path absence, and perform no preflight stage or retry.
