---
id: review-f1-rescope-r237-node-r
title: "F1 re-scope (Ruling 237): node R independent source review"
type: proof-pack
status: active
owner: "@pc-laptop-leader"
phase: preflight
tags: [f1, windows, arm64, review, adversary]
links:
  - { to: plan-f1-rescope-r237, rel: depends-on }
  - { to: proof-win-gpu-f1-rescope-verification, rel: depends-on }
  - { to: rulings, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  The two Adversary-mode reviews of the C1 to C5 source (change set 6f22dcb3..9e047b28) that plan node R convened on
  2026-10-19, recorded verbatim: the Test Architect (ACCEPT WITH CONDITIONS: the pin oracle, structural tests on the
  AST) and the Security & Identity Architect (ACCEPT WITH CONDITIONS: the contract read once, the protected identity
  under none, the retired legacy contract). The conditions and their disposition are in the plan's section 10.
---

# F1 re-scope: node R independent source review

Convened by the author session (pc-laptop-leader) on 2026-10-19 after node V, over the worktree at main `9e047b28`,
read-only, with the V receipt (`docs/proof/win-gpu-f1-preflight/rescope/verification-v.json`) as the proof pack. Each
reviewer ran the offline suites itself. The verdicts below are the reviewers' reports verbatim; the author's disposition
of every condition and advisory is the plan's section 10 and ledger (node R). The author did not clear any veto; no veto
was raised.

## 1. Test Architect (Adversary mode)

PERSONA: test-architect   MODE: Adversary   TIER: T2
VERDICT: PASS-WITH-CONDITIONS

**Verdict.** ACCEPT WITH CONDITIONS (two conditions, no hard veto). Every §5 test name exists and asserts what its name
says; both §10 conditions are honoured; the V receipt's counts mean what the plan says; the union is green at main
`9e047b28` on this host. No correctness claim is without a verification path, and no test that the ledger cites proves
nothing. Two tests prove less than their names claim and must be tightened before node A.

**F1, condition (Major, Verified).** The pin set has no oracle outside the code. `tests/test_f1_transaction_roots.py:65`
asserts `len(pins) == len(transaction.PINS) + 1` (tautology against `transaction.py:25-31`), and `check_pins`
(`transaction.py:39-46`) only requires that every present pin is a row. Failing input: delete `gfortran-13`,
`openmpi-bin`, `libopenmpi-dev`, `libblas-dev`, `liblapack-dev`, `pkg-config`, `gcc-13`, `g++-13` or
`openfoam2512-common/-source/-tools` from `PINS`: all three named C3 tests stay green (only `openfoam2512-dev`, the
`openfoam2512` version and `make` are probed, lines 79-92). The C3 claim "roots pinned: toolkit meta, OpenFOAM set,
compilers, MPI, BLAS/LAPACK, make:&lt;arch&gt;" therefore has a failing input for 3 of 15 pins. Clears when: the test asserts
`transaction.pinned_roots('arm64')` (and `'amd64'`) equal to a 15-element literal tuple carried by the test, and
`test_Roots_AbsentPin_Refuses` loops the deletion over every pin.

**F2, condition (Major, Verified).** The "structural halves" are text searches a comment satisfies.
`tests/f1_architecture.ps1:20-24` uses `$text.LastIndexOf('Assert-HostIdentity', $createIndex)`: a
`# Assert-HostIdentity` comment after the function definition passes it (and the real call at
`Invoke-Preflight.ps1:886` already satisfies it, so deleting the pre-install call at `:970` is undetected; harmless
today, but the test is not the control the plan names). `tests/f1_protected_none.ps1:72-75` counts regex matches of
`guard=$protectedRecord.guard` (≥2) and `$Protected -cne 'none'` (≥4) in raw text, comment-satisfiable likewise. Also
untested: that `Invoke-RootfsSignature` (`Invoke-Preflight.ps1:929`) precedes the guard start (`:950`) and
`'target-create'` (`:971`); Ruling 240 (2)'s "before target creation" is Verified only by my line read. Clears when: the
three suites search `$tokens` with `Kind -ne Comment` (the parser already returns them) or, better, find `CommandAst`
nodes outside function bodies and assert `StartOffset(Assert-HostIdentity) &lt; StartOffset(Invoke-RootfsSignature) &lt;
StartOffset(target-create)`.

**F3, advisory (Minor, Verified).** The V receipt was measured at `c174d4c3`, not at main `9e047b28`
(`verification-v.json` `head`). The diff since is `tools/run-f1-verification.py` only (+3/−2, the codec fix). I re-ran
the union's pieces at `9e047b28` (section 3): all green. Record the delta in §10 or regenerate the receipt at the join
HEAD.

**F4, advisory (Minor, Verified).** V's floors count files (`run-f1-verification.py:45-57`), not tests; a
`test_f1_x.py` with no test case or an `f1_x.ps1` that only prints satisfies them. The receipt does carry
`python_tests_ran=152`, `powershell_runs=20`, but nothing refuses a smaller number. Matches the plan's text ("file
counts"), so it means what the plan says; add test-count floors when the floors next move.

**F5, advisory (Minor, Verified).** `Refuses` helpers accept any exception (`f1_attempt2_evidence.ps1:12`,
`f1_protected_none.ps1:11`, `f1_derived_paths.ps1:16`). Mitigated: each negative is a one-variable delta from a
just-passed baseline (evidence `:35 → :40-42`; protected-none `:135 → :140-148`), so a refusal for the wrong reason
would require the baseline to pass by accident. Matching the message (`F1-LEADER-EVIDENCE-REFUSED`, `unapproved or
missing signing identity`) is the smallest tightening.

**F6, advisory (Minor, Verified).** `tools/check-f1-host-bindings.py:28-29` catches `C:\Projects` and `/mnt/c/` but not
`D:\Projects` or `/mnt/d/`: this laptop's own literal would pass the gate the plan calls "no host-bound literal".
Generalise to `[A-Za-z]:\\+Projects` and `/mnt/[a-z]/` (the self-test's `/mnt/[a-z]/*` glob at `:109` needs an
allowlist line).

**§10 conditions, honoured (Verified).** (1) `test_f1_transaction_roots.py:99-107`: B2 lock hash `2306d58e…` checked,
`package_count==135`, and `{(name, version)}` of `apt-simulate.stdout.txt` equals the lock's packages. (2)
`f1_attempt2_evidence.ps1:34` and `:42`: zero-byte stderr via `WriteAllBytes([byte[]]@())`; BOM-only (`:40`) and
whitespace (`:41`) refuse; the launcher check is byte length (`Invoke-Preflight.ps1:384`). Sweep claim confirmed: no
other must-be-empty stderr read in `Control.ps1` / `preflight.sh` (grep, zero hits).

**§5 names, all present and doing what they say (Verified).** Python: `test_f1_architecture.py:93,97,103,128,140,175,289`;
`test_f1_transaction_roots.py:60,76,96`; `test_f1_protected_none.py:38`; `test_f1_attempt2_prepare.py:77`. PowerShell:
`f1_architecture.ps1:41-42`; `f1_derived_paths.ps1:24-29,39,63,79`; `f1_protected_none.ps1:33-47,59-62,85-86,135-148`;
`f1_attempt2_evidence.ps1:40-50`. Literal tests carry their own literals (`test_f1_architecture.py:25-35`), the Detect
fixtures are the committed spike receipts, the emulated-5.1 case is a real measured shape (`:109-117`). Keyring
identity: Git's `gpg --show-keys` on the committed `inputs/ubuntu-cloudimage-keyring.gpg` lists exactly
`D2EB4462…7DB87C81` and `4A3CE3CD…476CF100` (the suite's `REAL_SIGNERS`); the fixture keyring holds only the throwaway
`F0ABAE66…A451C6DE`.

**What I ran** (worktree at HEAD `9e047b28`, `git status` clean before and after): `py -3 -m unittest discover -s tests
-p "test_f1_*.py" -v` → 152 ok, 0 FAIL/ERROR/skipped, EXIT=0. `pwsh -NoProfile -File
tools/run-f1-powershell-suites.ps1` → `F1-PS runtime=pwsh process=Arm64 native=Arm64`, same for `powershell`; 20 runs,
10 suites × 2 runtimes, all `exit 0`, 23477 ms, EXIT=0. `py -3 tools/run-f1-verification.py --self-test` → `PASS floors
python&gt;=9 powershell&gt;=9 today python=11 powershell=10`, EXIT=0. `check-f1-host-bindings.py` (16 files, 17
allowlisted), `check-wsl-inline.py`, `check-proof-pii.py` (0 hits), `check-docs.py` (`Documentation checks passed.`) →
all EXIT=0. Probe: a script that `throw`s under `-File` exits 1 on both `pwsh` and `powershell.exe` (so the runner's
exit propagation at `run-f1-powershell-suites.ps1:70` is real, not Inferred). `gpg --show-keys` on both keyrings
(read-only).

**Residual risks I consider acceptable.** Everything in plan §8 (nvcc/`sm_` list, arm64 rootfs bytes until gpgv runs on
the download, lock drift at `simulate`, index signatures, x64 live run and x64 `gpgv.exe` layout) is correctly labelled
Inferred and unreachable offline; each has a named refusal on the live path. Not in §8 but acceptable: the real Ubuntu
`SHA256SUMS` line shape (`*name`) is exercised only through the fixture (`Invoke-Preflight.ps1:142` admits `[ *]`), so
a format surprise refuses rather than admits (Inferred from the regex). The keyring's "reviewed" status is a human
property; offline the suites prove its contents equal the contract's allowlist and `verify_keyring` re-checks live
(Verified). `Get-HostFree` is tested through a shadowed `Get-PSDrive` (`f1_derived_paths.ps1:71`), a test of a mock,
but it asserts only which drive is asked, which is the behaviour C2 claims. Acceptable.

CLEARS-THE-VETO: yes. Traced tests: yes. Red-before-green: recorded in the ledger and suite docstrings, not re-observed
by me (Inferred). Proof pack attached: yes (`verification-v.json`, head `c174d4c3`; re-executed green at `9e047b28`
here). RESIDUAL RISK: F1 and F2 are the only places where a mutation of the shipped code survives the green suite; the
live-path risks are those of §8.

## 2. Security & Identity Architect (Adversary mode)

PERSONA: security-identity-architect   MODE: Adversary   TIER: T1
VERDICT: PASS-WITH-CONDITIONS

**Verdict.** ACCEPT WITH CONDITIONS. No hard veto. In the attempt-2 path every byte that reaches a WSL effect is pinned
(rootfs by `architecture.TABLE`, keyrings/sums/signature by `attempt.INPUT_HASHES`, signers by `attempt.SIGNERS`, packet
by `packet_sha256`) and the contract bytes are bound into a ruling on `origin/main` (`attempt.check_ruling`). Three
conditions below; none lets unreviewed bytes reach WSL on the attempt-2 path.

**F1, condition (Major, Verified): contract TOCTOU; launcher fields come from bytes `verify` never saw.**
`Invoke-Preflight.ps1:853` and `:866` parse `$contractData` from the file; `contract.py verify` (`:875-885`) re-reads
and checks the file against the committed blob; `$script:ContractSha` is hashed at `:878` after verify. `$Protected`
(`:876`), host floors (`:875`), `architecture` (`:884`), `inputs.rootfs.sha256` (`:930`), `target_path` (`:188`) all
come from the `:866` parse. A write between `:866` and verify's read, or between verify and `:878`, gives a mixed state
(PowerShell acts on one document; `keyring`/`signer` later validate another against `ContractSha`). Attack/mistake:
same-user writer (an editor save mid-launch). Clears when: read bytes once (`ReadAllBytes`), hash them, parse from
them, and pass `--expected-sha256` to `verify` so contract.py refuses if the file's bytes differ (it already does this
for `keyring`/`signer`).

**F2, condition (Major, Verified): `none` is admitted on a host that carries the protected distro.** `Control.ps1`
`Assert-FreshRegistrations`: `if ($ProtectedState -ceq 'none') { continue }` skips the name/GUID/path alias check;
`tests/f1_protected_none.ps1:37` asserts a row named `cfdw-openfoam2512` is admitted under `none`; the final precheck
(`Invoke-Preflight.ps1:899`) likewise skips. Ruling 240 (1) literally allows this ("neither required nor refused"), so
it is not weaker than the ruling, but the ruling's justification is vacuity "on a host that does not carry that
distro" and the launcher never proves that. Effect: a `none` contract on the old PC runs with no keepalive requirement
and no completed-L3 guard (the Ruling 217/225 protection). Mitigant (Verified): `Assert-AttemptExclusivity` still
refuses a running keepalive as a foreign WSL actor; a B2 host with the keepalive down is not refused. Clears when:
under `none`, refuse any row whose `DistributionName`, `PSChildName` or `BasePath` matches the protected identity
(mirror the existing `elseif` alias branch), with a negative test, and a one-line amendment to Ruling 240 (1) recorded
by the operator.

**F3, condition (Major, Verified, pre-existing, outside the diff): the legacy non-attempt contract path is still live
and unbound.** `contract.py:76-79` (`elif execution_branch` matches `win/gpu-f1-r217-*`), `:139` (`run-1` path),
`Control.ps1 Get-InvocationRunRoot` (run-1 default), `Invoke-Preflight.ps1:840` (`$freshAttempt` false → no
`-Authorization`). Present at `6f22dcb3` (`contract.py:75-78`). On this path `allowed_signers` only needs the
OpenFOAM/NVIDIA anchors (extra fingerprints admitted), the keyring/sums/signature hashes are whatever the contract
states (only rootfs is pinned to `TABLE`), no ruling binds the bytes, and the leader check is any live epoch equal to
the contract's. Effect: an unreviewed OpenFOAM/NVIDIA keyring reaches apt in the guest (repositories/simulate/download,
network, no install). The C4 PS test builds its contract from `run-1/invocation-contract.json` and passes
`check_contract_fields` with a throwaway signer only because of this branch. Clears when: `check_contract_fields`
requires `attempt` (retire the branch) or the run-1 path gets the same ruling binding; parent decides whether this is
node R scope or a follow-up finding.

**F4, advisory (Minor, Flagged): keyring provenance is a version string, not a source.** `packet/README.md:203` records
"ubuntu-keyring 2023.11.28.1" + SHA-256; commit `b4092d08` is the only provenance; no upstream URL/package hash.
Mitigant (Verified by `gpg --show-keys`): the committed keyring holds exactly `D2EB…7C81` (UEC Image Automatic Signing
Key) and `4A3C…F100` (Ubuntu Cloud Image Builder), both in `SIGNERS['ubuntu_rootfs']`, no subkeys; README already
marks the signer sets Flagged until independently reviewed. Clears when: `inputs/README` cites the upstream
package/URL and its hash, and a reviewer confirms the two fingerprints against Ubuntu's published key page.

**F5, advisory (Minor, Verified): packet TOCTOU host-mount (pre-existing).** `Invoke-Preflight.ps1:905-908` hashes the
packet at precheck; the guest re-reads `architecture.py`/`transaction.py` from the host mount at each stage
(`preflight.sh`), minutes later. Same user domain. Clears when: copy the packet into `$Live` and hash/run the copy.

**F6, advisory (Nit, Verified): `verify_keyring` runs Git's `gpg.exe` with the user's homedir/gpg.conf**
(`contract.py:verify_keyring`), which may create `%APPDATA%\gnupg`. Add `--no-options --homedir &lt;temp&gt;` for
determinism.

**Non-findings, checked and closed (Verified).** `Convert-MsysPath`/`Convert-LinuxPath` reject relative paths and
whitespace; all seven inputs pass `Convert-LinuxPath` at `:831-834` before use, so no option injection into gpgv/py
(every argument is an absolute drive path); `gpgv --keyring` consults only the given keyring (fixture signature:
`NO_PUBKEY` against the reviewed keyring, `VALIDSIG` against the fixture keyring); `signer_is_allowed` requires every
`VALIDSIG` fingerprint in the allowlist, so a keyring swap between `keyring` and `gpgv` cannot pass (fingerprint = key
identity); fixture keyring hash `afa523…` ≠ pinned `c2d40d…` and fixture signer `F0AB…` ∉ `SIGNERS`, and
`check_binding` requires exact `INPUT_HASHES` and `SIGNERS`, so no fixture can be substituted into an attempt-2
contract; host identity uses CIM as ground truth and refuses an emulated launcher; `target_path` is derived from the
script location and must equal the contract's, `..` and whitespace refused; `prepare` uses exclusive `open('xb')` on
both files with rollback; `check_pins` runs before any index read and every lock row must match the host
architecture; the hand-quoted `--gpg` path arrives as one argument.

**What I ran.** `py -3 -m unittest discover -s tests -p "test_f1_*.py"` → `Ran 152 tests in 33.557s OK`, exit 0.
`pwsh -NoProfile -File tools/run-f1-powershell-suites.ps1` → `F1-PS total 20 runs (10 suites x 2 runtimes) 23450 ms`,
`all suites passed on both runtimes`, exit 0. A probe script (pwsh 7.6.7): the py child received
`['--kind','ubuntu_rootfs','--gpg','C:\\Program Files\\Git\\usr\\bin\\gpg.exe']`, list-form `subprocess.run` of that
path → `rc 0 gpg (GnuPG) 2.4.9`; gpgv fixture signature vs reviewed keyring → exit 2, `ERRSIG … NO_PUBKEY
37FAD5AAA451C6DE`; vs fixture keyring → exit 0, `VALIDSIG F0ABAE6667730DD5ED887B2A37FAD5AAA451C6DE`; `gpg
--show-keys` of the reviewed keyring → two `fpr` lines `D2EB…7C81`, `4A3C…F100`, no `sub`. `sha256sum`: reviewed
keyring `c2d40d…bf2f` (= `INPUT_HASHES`), fixture keyring `afa523…d7c7`. No WSL, registry, network or install touched;
no repo file created or edited.

**Residual risk accepted.** Trust domain is the signed-in user: every pinned byte lives in the user's own checkout and
the ruling binding is a local `origin/main` ref (no fetch); a local same-user attacker is out of scope by design; F1/F5
are mistake-class windows inside that domain. gpgv 2.4.9 on a shipped path: no known-exploitable CVE I can cite
offline (Inferred; network check not permitted). Fixture secret key "discarded with its temporary home": Inferred,
unverifiable; harmless since the fingerprint is not allowlisted anywhere binding. The guest runs as root inside a
disposable distro with the host mount visible (pre-existing design, Rulings 217/225).

CLEARS-THE-VETO: yes. Boundaries named (contract/ruling, keyring+signer allowlist, host identity, derived path, guest
architecture); input validated trusted-side (sha pins, fingerprint allowlists, path grammar); no secrets in
source/logs; no exploitable CVE on a shipped path (Inferred, offline); least privilege: user-level launcher, disposable
root guest. RESIDUAL RISK: same-user local race windows (F1, F5); `none` on a B2 host with keepalive down (F2); legacy
run-1 path (F3) until retired.

## 3. Cycle 2: confirmations (2026-10-19, worktree at 26faf712)

**Test Architect.** CONFIRMED for the four items (F1, F2, F3, F6), each Verified by mutation: dropping `gfortran-13`,
`libopenmpi-dev` or `openfoam2512-tools=2512.0-2` from `transaction.PINS` in memory fails two cases each; in a scratch
copy of the launcher, the pre-install `Assert-HostIdentity` turned into a comment, one receipt's `guard` pair replaced,
and the signature moved after the guard each fail the AST suites (baseline copies green). Receipt `head` = `1b05e892`,
`green`, `worktree_clean`, 152 tests / 20 runs. Ran at `26faf712`: Python 152 OK; 20 PowerShell runs all exit 0;
host-bindings self-test and gate; the verification runner's self-test. One new finding from the repair (condition,
Major, Verified): the `--expected-sha256` path had no failing input (no Python test passed `expected_sha256`; the
admission stub ignored the argument). F4 and F5 stand as open advisories for node A.

**Security & Identity Architect.** CONFIRMED (F1, F2, F3, F4, F6 met, each Verified; probe: `verify
--expected-sha256 ffff…` refuses with `invocation contract byte/hash mismatch`, exit 5, before the branch check; the
correct hash proceeds to `execution branch mismatch`). `contract.py` still carries the legacy `elif` for the run-1
branch shape, but it no longer reaches a WSL effect: acceptable. Ran at `26faf712`: Python 152 OK; 20 PowerShell
runs all passed. One gap to carry, not a cycle-3 trigger: the Python half of F1 had no negative unit test. F5 remains
open for node A.

**Disposition (author, same cycle).** The joint follow-up is applied: `tests/test_f1_attempt_admission.py`
`RealVerify_ExpectedSha256_BoundToParsedBytes` (a wrong hash refuses before any process boundary and before the
authorization check; the right hash proceeds to the next refusal) and the admission stub in
`tests/f1_attempt_controls.ps1` requires the forwarded `--expected-sha256` to equal the hash of the file it wrote,
on both runtimes. The track's repair cap (2 cycles) was not reached: cycle 1 applied the conditions, cycle 2
confirmed them.
