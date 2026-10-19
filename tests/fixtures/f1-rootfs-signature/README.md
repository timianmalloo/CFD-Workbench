# Rootfs signature fixture (node C4, Ruling 240 (2))

Read by `tests/f1_protected_none.ps1` (`Rootfs_UnapprovedSigner_RefusedOnWindows`). The Windows-side rootfs
signature check (`Invoke-Preflight.ps1` `Invoke-RootfsSignature`) runs the Git-for-Windows `gpgv.exe` over these
files exactly as it would over Ubuntu's `SHA256SUMS` and `SHA256SUMS.gpg`.

| File | What it is |
|---|---|
| `fixture-keyring.gpg` | the public key of a throwaway ed25519 key, fingerprint `F0ABAE6667730DD5ED887B2A37FAD5AAA451C6DE` |
| `SHA256SUMS` | two lines naming the amd64 and arm64 rootfs file names with the SHA-256 of the bytes `fixture-rootfs\n` (`89fa28756c61ffb898294d920fc9fce84168e0eceeff5389fabf074bfd8d8989`) |
| `SHA256SUMS.gpg` | the detached signature of `SHA256SUMS` by that key |

Generated 2026-10-19 inside WSL Ubuntu-24.04 (`gpg --quick-generate-key ... ed25519 sign never`, `gpg --export`,
`gpg --detach-sign`); the secret key was discarded with its temporary home. The key is not an Ubuntu signer: a contract
whose `allowed_signers.ubuntu_rootfs` carries the real cloud-image fingerprints refuses this signature, which is the
test's point. Nothing here is a reviewed input of the packet.
