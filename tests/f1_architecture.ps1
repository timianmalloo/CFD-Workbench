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
foreach ($name in @('Write-Lf', 'Write-Json', 'Open-LiveEvidenceStream', 'Read-LiveText', 'Get-GpgvPath', 'Get-GpgvObservation', 'Write-HostObservations', 'Assert-HostIdentity')) {
    $node = $ast.Find({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name }, $true)
    if ($null -eq $node) { throw "launcher function missing: $name" }
    $definitions[$name] = $node
    Invoke-Expression $node.Extent.Text
}
# TargetCreate_WithoutHostIdentity_Refuses, structural half on the AST (never raw text: a comment cannot satisfy it; node
# R, Test Architect F2): in the launcher body, outside every function, Assert-HostIdentity is called before
# Invoke-RootfsSignature, the guard starts after the signature, Assert-HostIdentity is called again after the guard, and
# the one top-level 'target-create' literal (the launch site; the other is Invoke-Child's catch branch) comes last.
function Test-InFunction($node) {
    $parent = $node.Parent
    while ($null -ne $parent) { if ($parent -is [Management.Automation.Language.FunctionDefinitionAst]) { return $true }; $parent = $parent.Parent }
    return $false
}
function Get-TopLevelCallOffsets([string]$CommandName) {
    $nodes = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.CommandAst] -and $n.GetCommandName() -ceq $CommandName }, $true) | Where-Object { -not (Test-InFunction $_) })
    if ($nodes.Count -lt 1) { throw "launcher body does not call $CommandName" }
    return @($nodes | ForEach-Object { $_.Extent.StartOffset })
}
$createLiterals = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.StringConstantExpressionAst] -and $n.Value -ceq 'target-create' }, $true) | Where-Object { -not (Test-InFunction $_) })
if ($createLiterals.Count -ne 1) { throw "expected one top-level 'target-create' launch site, found $($createLiterals.Count)" }
$createOffset = $createLiterals[0].Extent.StartOffset
$identityOffsets = Get-TopLevelCallOffsets 'Assert-HostIdentity'
$signatureOffset = (Get-TopLevelCallOffsets 'Invoke-RootfsSignature' | Measure-Object -Minimum).Minimum
$guardStarts = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -ceq '$Guard' -and $n.Right.Extent.Text -like 'Start-Process*' }, $true) | Where-Object { -not (Test-InFunction $_) })
if ($guardStarts.Count -ne 1) { throw "expected one top-level guard start, found $($guardStarts.Count)" }
$guardOffset = $guardStarts[0].Extent.StartOffset
$firstIdentity = ($identityOffsets | Measure-Object -Minimum).Minimum
$lastIdentity = ($identityOffsets | Measure-Object -Maximum).Maximum
if (-not ($firstIdentity -lt $signatureOffset -and $signatureOffset -lt $guardOffset -and $guardOffset -lt $lastIdentity -and $lastIdentity -lt $createOffset)) {
    throw "launcher order is not host identity ($firstIdentity), rootfs signature ($signatureOffset), guard ($guardOffset), host identity again ($lastIdentity), target creation ($createOffset)"
}

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
