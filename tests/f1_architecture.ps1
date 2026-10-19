# F1 re-scope node C1 (Rulings 237, 238 (1), 240): the launcher records this host's architecture, CPU and GPU before the
# first WSL effect and refuses target creation without that receipt or with a receipt for another architecture.
# Offline-fast; run on both runtimes (Windows PowerShell 5.1 and pwsh). The observations are real (RuntimeInformation,
# CIM); nothing touches WSL. The host's own architecture is recorded, never asserted (HOST-ARCH-ASSERTED).
$ErrorActionPreference = 'Stop'; Set-StrictMode -Version Latest
$packet = Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\proof\win-gpu-f1-preflight\packet'
$errors = $null; $tokens = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'launcher parse failed' }
$definitions = @{}
foreach ($name in @('Write-Lf', 'Write-Json', 'Open-LiveEvidenceStream', 'Read-LiveText', 'Write-HostObservations', 'Assert-HostIdentity')) {
    $node = $ast.Find({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name }, $true)
    if ($null -eq $node) { throw "launcher function missing: $name" }
    $definitions[$name] = $node
    Invoke-Expression $node.Extent.Text
}
# TargetCreate_WithoutHostIdentity_Refuses, structural half: the launcher body calls Assert-HostIdentity (outside its
# definition) before the 'target-create' child starts (the last 'target-create' literal is the launch site; the
# earlier one is Invoke-Child's catch branch).
$text = $ast.Extent.Text
$createIndex = $text.LastIndexOf("'target-create'")
$callIndex = $text.LastIndexOf('Assert-HostIdentity', $createIndex)
$definitionEnd = $definitions['Assert-HostIdentity'].Extent.EndOffset
if ($createIndex -lt 0 -or $callIndex -lt $definitionEnd) { throw 'target creation is not preceded by an Assert-HostIdentity call' }

function Invoke-Py([string[]]$Arguments, [string]$Root) {
    $out = Join-Path $Root 'py.stdout.txt'; $err = Join-Path $Root 'py.stderr.txt'
    $process = Start-Process -FilePath 'py' -ArgumentList $Arguments -PassThru -NoNewWindow `
        -RedirectStandardOutput $out -RedirectStandardError $err
    $null = $process.Handle
    $process.WaitForExit()
    return $process.ExitCode
}

$tmp = Join-Path ([IO.Path]::GetTempPath()) ('cfdw-f1-architecture-' + [Guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Path $tmp | Out-Null
try {
    $Live = $tmp
    $contractData = [pscustomobject]@{ architecture = 'arm64' }
    $failed = $false
    try { $null = Assert-HostIdentity } catch { $failed = $true }
    if (-not $failed) { throw 'TargetCreate_WithoutHostIdentity_Refuses: an absent receipt was admitted' }

    $observations = Join-Path $tmp 'host-observations.json'
    Write-HostObservations $observations
    $data = [IO.File]::ReadAllText($observations) | ConvertFrom-Json
    if ($data.schema -cne 'cfdw-f1-host-observations/1' -or [string]::IsNullOrEmpty($data.os_architecture) -or
        [string]::IsNullOrEmpty($data.os_architecture_cim) -or @($data.cpu).Count -lt 1 -or @($data.gpu).Count -lt 1) {
        throw 'host observations incomplete'
    }
    # No second copy of the table here: architecture.py decides which token, if any, this runtime may record. A
    # runtime that runs under emulation (Windows PowerShell 5.1 started from an x64 parent on the ARM64 laptop,
    # measured 2026-10-19) is refused for both tokens, with the host still recorded.
    $identity = Join-Path $tmp 'host-identity.json'
    $script = Join-Path $packet 'architecture.py'
    $expected = ''
    foreach ($candidate in @('amd64', 'arm64')) {
        $exit = Invoke-Py @('-3', $script, 'host-identity', '--observations', $observations, '--output', $identity, '--expected-architecture', $candidate) $tmp
        if ($exit -eq 0) { $expected = $candidate; break }
        $receipt = [IO.File]::ReadAllText($identity) | ConvertFrom-Json
        if ([string]::IsNullOrEmpty($receipt.refusal) -or @($receipt.cpu).Count -lt 1) { throw 'a refused receipt lost its refusal or the host record' }
        Remove-Item -LiteralPath $identity
    }
    if (-not $expected) {
        if ($receipt.refusal -notmatch 'emulation' -or $null -ne $receipt.architecture) { throw "both tokens refused for another reason: $($receipt.refusal)" }
        [IO.File]::WriteAllText($identity, (ConvertTo-Json $receipt -Depth 6))
        $contractData = [pscustomobject]@{ architecture = 'arm64' }
        $failed = $false
        try { $null = Assert-HostIdentity } catch { $failed = $true }
        if (-not $failed) { throw 'a refused (emulated) receipt was admitted' }
        Write-Output "f1 architecture PASS runtime=$($PSVersionTable.PSVersion) process=$($data.process_architecture) cim='$($data.os_architecture_cim)': this runtime is emulated; its receipt is refused for both tokens with the host recorded, and target creation refuses it"
        return
    }
    $other = if ($expected -ceq 'amd64') { 'arm64' } else { 'amd64' }
    $contractData = [pscustomobject]@{ architecture = $expected }
    $token = Assert-HostIdentity
    if ($token -cne $expected) { throw "Assert-HostIdentity returned '$token' for a $expected receipt" }
    $contractData = [pscustomobject]@{ architecture = $other }
    $failed = $false
    try { $null = Assert-HostIdentity } catch { $failed = $true }
    if (-not $failed) { throw 'a receipt for another architecture was admitted' }

    Remove-Item -LiteralPath $identity
    $exit = Invoke-Py @('-3', $script, 'host-identity', '--observations', $observations, '--output', $identity, '--expected-architecture', $other) $tmp
    if ($exit -eq 0) { throw 'a contract for the other architecture was accepted' }
    $receipt = [IO.File]::ReadAllText($identity) | ConvertFrom-Json
    if ($receipt.architecture -cne $expected -or [string]::IsNullOrEmpty($receipt.refusal)) { throw 'the refused receipt lost the host record' }
    $contractData = [pscustomobject]@{ architecture = $expected }
    $failed = $false
    try { $null = Assert-HostIdentity } catch { $failed = $true }
    if (-not $failed) { throw 'a refused receipt was admitted' }
    Write-Output "f1 architecture PASS runtime=$($PSVersionTable.PSVersion) host=$expected cpu=$(@($data.cpu).Count) gpu=$(@($data.gpu).Count); absent, other-architecture and refused receipts refused before target creation"
} finally {
    Remove-Item -Recurse -Force -LiteralPath $tmp -ErrorAction SilentlyContinue
}
