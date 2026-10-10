$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$proofRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$cache = Join-Path $proofRoot '.source-cache'
New-Item -ItemType Directory -Path $cache -Force | Out-Null

$sources = [ordered]@{
    'ubuntu-noble-SHA256SUMS' = 'https://cloud-images.ubuntu.com/wsl/releases/noble/current/SHA256SUMS'
    'ubuntu-noble-SHA256SUMS.gpg' = 'https://cloud-images.ubuntu.com/wsl/releases/noble/current/SHA256SUMS.gpg'
    'ubuntu-noble-wsl-amd64-wsl.manifest' = 'https://cloud-images.ubuntu.com/wsl/releases/noble/current/ubuntu-noble-wsl-amd64-wsl.manifest'
    'ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz' = 'https://cloud-images.ubuntu.com/wsl/releases/noble/current/ubuntu-noble-wsl-amd64-wsl.rootfs.tar.gz'
    'cuda-keyring_1.1-1_all.deb' = 'https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/cuda-keyring_1.1-1_all.deb'
    'cuda-ubuntu2404-InRelease' = 'https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/InRelease'
    'cuda-ubuntu2404-Packages.gz' = 'https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/Packages.gz'
    'cuda-eula.html' = 'https://docs.nvidia.com/cuda/eula/index.html'
}

$records = @()
foreach ($entry in $sources.GetEnumerator()) {
    $target = Join-Path $cache $entry.Key
    Invoke-WebRequest -UseBasicParsing -Uri $entry.Value -OutFile $target
    $records += [ordered]@{
        name = $entry.Key
        url = $entry.Value
        bytes = (Get-Item -LiteralPath $target).Length
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash.ToLowerInvariant()
    }
}
$json = $records | ConvertTo-Json -Depth 4
[IO.File]::WriteAllText(
    (Join-Path $proofRoot 'source-downloads.json'),
    ($json -replace "`r`n", "`n") + "`n",
    [Text.UTF8Encoding]::new($false)
)
