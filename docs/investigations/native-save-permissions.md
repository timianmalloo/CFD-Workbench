---
id: investigation-native-save-permissions
title: Native save creates a file with unintended permissions
type: doc
status: in-review
owner: "@cfd-application-20260923"
phase: implementation
tags: [application, persistence, native-interop, investigation]
links:
  - {to: design-application-contracts, rel: depends-on}
  - {to: review-ui-application-native, rel: relates-to}
review-by: 2026-10-23
summary: >-
  A real macOS Save created mode 0454 despite the store requesting 0600.
  A controlled ABI probe isolates fixed-versus-variadic argument passing;
  the managed production repair still requires independent native proof.
---

# Native save permission investigation

Goal: explain and repair the native Save permission mismatch without weakening
directory-relative publication, identity, durability or cleanup guarantees.
Done when a managed, packaged save creates the intended permissions from initial
inode creation onward and independent regression/security review passes.
Out of scope: Windows persistence implementation, format changes and unrelated UI.
Tier T2. Root investigates and independently reviews; the isolated persistence
author implements only after the technical Owner's routing decision. The user's
standing authorization delegates technical repair decisions to that Owner; no
additional human permission is required for this bounded repair.

## Observed failure and binding

The native CUA Save flow on 23 September 2026 created
`/tmp/cfd-native-review.czEz9w/accepted.cfdw.json`, 7836 bytes, and reported
“Saved and durability confirmed.” The same picker reopened it with unchanged
source SHA `8d5f26ac7efa01acaea9c1b8571f20e91a94a5dc5e6e2aaf3ae4fda844298cfa`
and accepted revision `62360ae5-cf8b-4650-bb0d-080505195283`.

Independent `stat` reported **0454**, owner 501, group 0. The private parent is
0700. Saved-file SHA-256 is
`b5fa3b7cdc6360ce559881caa443b4f44e3db3bb4c9d38ecff8d6f902c5ce150`.
This artifact is retained unchanged, including its wrong mode. A readback proves
content survived; it does not validate permission semantics.

The running review copy is bound in the [native review](../reviews/ui-application-native.md):
PID 48841, start `Wed Sep 23 13:24:28 2026`, Desktop DLL
`6aaff522eec3da85898685992fd3f2e3325035e2c482b9edda76b6dc42f54aa9`.
Its launch receipt binds Persistence DLL
`6093d1d1c252c290d999ae11c64a6065decb0ecdbd4bc9f037c1166deea4fbd7`.
The shell umask was observed as 022; the app process's umask was not separately
measured. The probe below sets its own umask explicitly.

## Hypotheses and controlled comparison

The inspected `ProjectStore.cs` calls `OpenAt` with create/exclusive/no-follow
flags and mode `0x180` (0600). Its P/Invoke declares four fixed parameters.
The native `openat` contract has three fixed parameters and a variadic mode.
[Apple's ARM64 contract](https://developer.apple.com/documentation/xcode/writing-arm64-code-for-apple-platforms)
distinguishes variadic argument placement. The
[Python ctypes documentation](https://docs.python.org/3/library/ctypes.html#calling-variadic-functions)
requires describing the fixed parameters correctly on Apple ARM64.

Root ran `/tmp/cfd-native-review.czEz9w/abi_probe.py` on macOS 27 ARM64,
Python 3.14.4. It creates only empty, exclusive files inside the private task
directory. Library entrypoint, parent descriptor, flags, requested mode and
umask are identical; only the declared number of fixed parameters changes.

| Declaration | Requested mode | Measured mode, run 1 / run 2 |
|---|---|---|
| Four fixed parameters | 0600 | 0000 / 0000 |
| Three fixed parameters plus variadic mode | 0600 | 0600 / 0600 |

Receipt: `/tmp/cfd-native-review.czEz9w/abi-probe-receipt.json`. The probe took
0.013845 seconds and confirmed that the original saved file's bytes and mode
were unchanged. Umask was explicitly 022 and restored on exit. An umask can
remove permission bits; it cannot add the group execute/read and other read
bits observed in the native Save. This disconfirms umask alone as the cause.

**Verified:** misdeclaring this native variadic call reproduces unintended
creation permissions, and correcting the declaration removes that failure in
the controlled probe. **Inferred, strongly supported:** the production fixed
P/Invoke causes its 0454 result. The probe is Python/libffi, not a managed
production correction; final causal closure requires the actual repaired
.NET/store/package path to pass the same permission oracle. Incorrect values
need not match across runtimes because the callee reads the wrong argument slot.

## Class, sweep and repair phases

Class: an FFI declaration that matches apparent parameter types but violates
the platform ABI for a variadic API. Byte-correct round trips and zero exit
codes fail to detect corrupted side-effect metadata.

The sibling sweep found fixed declarations for both `Open` and `OpenAt`.
Current `Open` use opens an existing directory without creation; the defective
mode path is `OwnedEntry.Create`, shared by temporary data and cooperative
claim files. The author must sweep every native declaration/callsite against
its actual system signature and record which are variadic. Do not infer that
all native methods are defective from this pair.

1. Freeze a managed production RED that asserts permissions before first write,
   on temporary/claim inodes and final publication, using independent OS stat.
2. Spike an ABI-correct boundary using the installed compiler/runtime and
   actual package layout. The Owner selects the repair; do not replace the
   mismatch with post-creation chmod or depend on an undocumented symbol.
3. Implement only the bounded store/interoperability changes; preserve native
   error mapping, no-follow/exclusive flags, descriptor-relative publication,
   collision checks and exact-owned cleanup. Check both new and overwrite saves.
4. Run targeted regressions and existing store gates, then root's independent
   packaged native Save and mode checks. Root retains the Security/Data veto;
   the Owner cannot clear its own authoring decision.

Residuals: Windows remains separately unassessed; correcting mode bits does not
prove ACL inheritance, release signing or every filesystem's durability. A
managed comparison that fails to reproduce the mismatch or a corrected ABI
that retains it would reopen the diagnosis. No production code was changed by
this investigation, and this report does not accept the store repair.

## R26 repaired managed candidate: independent checkpoint

Owner selected a minimal fixed-signature C bridge, compiled against the installed
system headers. The isolated author retained a managed before-first-write RED
(0456 instead of 0600), then the corrected packaged test and mutation results.
Those author results are evidence to review, not a substitute for root execution.

Root inspected the corrected source: both variadic boundaries now sit behind
fixed C exports; assembly-directory loading is explicit; unsupported architecture
and missing/unloadable helper fail closed. An exact held-fstat 0600 guard runs
before writing project bytes. A weaker draft of that guard admitted 0000 and was
rejected during review; zero and owner-bit-loss cases are required regressions.
No post-creation chmod masks the defect.

Root's separate public-API consumer against frozen Persistence DLL
`c3245fbae2ebed15d488f56613e15099958b212ba30638846797a91266b56e71`
and helper `90181814d8acebf1b322579d0ff41964f63311e946b34c3df1c04698e9b24c88`
verified create, overwrite, exact native image bytes/hash and accepted revision
under child umasks 0000, 0022 and 0077. Independent Python OS stat measured 0600
each time. Owner-read-stripping umask 0400 returned
`DOC-UNSUPPORTED-PERSISTENCE`, with no published or residual file. Parent
directories were created before applying each child mask. Original 0454 evidence
and frozen assemblies were verified unchanged.

Receipt: `/tmp/cfd-native-review.czEz9w/store-consumer/receipts/independent-store.json`.
The [native review](../reviews/ui-application-native.md) records process ownership,
timings and boundaries. **Verified:** the repaired managed production store path
now passes these independent permission and round-trip cases. **Still due:**
actual Save/Reopen through the combined packaged UI, author-proof reconciliation
and final independent disposition. This checkpoint does not accept B/C/M1.
