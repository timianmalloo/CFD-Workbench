# Offline-admission ring: pure production controls and pre-WSL launch sentinel.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$packet = Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\proof\win-gpu-f1-preflight\packet'
. (Join-Path $packet 'Control.ps1')
function Require($condition, $message) { if (-not $condition) { throw $message } }
function Refuses([scriptblock]$action) { $refused = $false; try { & $action | Out-Null } catch { $refused = $true }; Require $refused 'invalid identity accepted' }
$path = 'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
$target = [pscustomobject]@{ DistributionName='cfdw-petsc-cuda132-f1'; PSChildName='{11111111-2222-3333-4444-555555555555}'; Version=2; BasePath=$path }
$protected = [pscustomobject]@{ DistributionName='cfdw-openfoam2512'; PSChildName='{b7566a82-9cee-47ce-a246-f83632353cea}'; Version=2; BasePath=[Environment]::ExpandEnvironmentVariables('%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512') } # machine-path-ok: synthetic fixture expansion; no expanded value is captured.
Require ((Get-BoundTargetKey @($target) $path $target.PSChildName) -ceq $target.PSChildName) 'new GUID rejected'
foreach ($field in @('DistributionName','PSChildName','BasePath','Version')) {
    $bad = $target.psobject.Copy()
    $bad.$field = if ($field -eq 'Version') { 1 } else { 'changed' }
    Refuses { Get-BoundTargetKey @($bad) $path $target.PSChildName }
}
foreach ($guid in @($protected.PSChildName, '{e958c4d5-ae78-4b50-a634-7102f334d3aa}')) {
    $bad = $target.psobject.Copy(); $bad.PSChildName = $guid
    Refuses { Get-BoundTargetKey @($bad) $path }
}
Assert-FreshRegistrations @($protected) @('cfdw-openfoam2512') $false 1 $path
Refuses { Assert-FreshRegistrations @($protected, $target) @('cfdw-openfoam2512') $false 1 $path }
Refuses { Assert-FreshRegistrations @($protected) @('cfdw-openfoam2512') $true }
$renamed = $target.psobject.Copy(); $renamed.DistributionName='renamed'; $renamed.BasePath='moved'; $renamed.PSChildName='{e958c4d5-ae78-4b50-a634-7102f334d3aa}'
Refuses { Assert-FreshRegistrations @($protected, $renamed) @('cfdw-openfoam2512') $false 1 $path }
$alias = $target.psobject.Copy(); $alias.DistributionName='alias'; $alias.BasePath=$protected.BasePath
Refuses { Assert-FreshRegistrations @($protected, $alias) @('cfdw-openfoam2512') $false 1 $path }
$alias.BasePath='other'; $alias.PSChildName=$protected.PSChildName
Refuses { Assert-FreshRegistrations @($protected, $alias) @('cfdw-openfoam2512') $false 1 $path }
$data = [pscustomobject]@{schema='cfdw-f1-r217-invocation/2';architecture='amd64';host_start_floor_bytes=19684091961;host_reserve_bytes=10737418240;attempt=1;leader_epoch=8;execution_branch='win/gpu-f1-r225-attempt1';session='pc-f1-r225-attempt1';run_id='r225-attempt1-epoch8-20261012';run_root='docs/proof/win-gpu-f1-preflight/attempt-1'}
Require ((Get-InvocationRunRoot $data 'C:\Projects\fixture' $data.run_id $data.session) -like '*attempt-1') 'fresh routing rejected'
Refuses { Get-InvocationRunRoot $data 'C:\Projects\fixture' 'wrong-run' $data.session }
# Execute the actual entrypoint admission span. Only the child boundary is inert.
$parseErrors = $null; $tokens = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'), [ref]$tokens, [ref]$parseErrors)
Require ($parseErrors.Count -eq 0) 'entrypoint syntax failed'
$start = $ast.EndBlock.Statements | Where-Object { $_.Extent.Text.StartsWith('$contractData = [IO.File]::ReadAllText($Contract)') } | Select-Object -First 1
$finish = $ast.EndBlock.Statements | Where-Object { $_.Extent.Text.StartsWith('if ($precheck.ExitCode -ne 0)') } | Select-Object -First 1
Require ($null -ne $start -and $null -ne $finish) 'normal admission span absent'
$source = [IO.File]::ReadAllText((Join-Path $packet 'Invoke-Preflight.ps1'))
$admission = [scriptblock]::Create($source.Substring($start.Extent.StartOffset, $finish.Extent.EndOffset-$start.Extent.StartOffset))
$RepoRoot = Split-Path -Parent $PSScriptRoot
$PacketRoot = $packet
$RunId=$data.run_id; $AgentSession=$data.session; $ExclusiveRunId=$RunId
$TargetPath=$path
function Assert-TargetPath { return $TargetPath }
function py { return 'offline authority scanner boundary' }
$global:LASTEXITCODE=0
$script:wslCalls=0
$script:admissionChildren=0
function Invoke-Child { $script:wslCalls++; throw 'WSL-SENTINEL: no real launch permitted' }
function Start-Process {
    param($FilePath, $ArgumentList, $RedirectStandardOutput, $RedirectStandardError, [switch]$PassThru, $WindowStyle)
    if ($FilePath -ceq 'wsl.exe') { $script:wslCalls++; throw 'WSL-SENTINEL: no real launch permitted' }
    Require ($FilePath -ceq 'py' -and $ArgumentList -contains '--run-id') 'normal entrypoint lost contract/RunId forwarding'
    $script:admissionChildren++
    $authAt = [Array]::IndexOf($ArgumentList, '--authorization')
    Require ($authAt -ge 0) 'entrypoint lost authorization forwarding'
    & pyOriginal -3 (Join-Path $PSScriptRoot 'f1_attempt_authority_probe.py') $Contract $ArgumentList[$authAt+1] $PacketRoot 2> $RedirectStandardError
    $exit = $LASTEXITCODE
    [pscustomobject]@{Handle=1;HasExited=$true;ExitCode=$exit;WaitForExit={param($milliseconds) return $true}} | ForEach-Object { $_ | Add-Member ScriptMethod WaitForExit {param($milliseconds) return $true} -Force; $_ }
}
Set-Alias pyOriginal (Get-Command py.exe).Source
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('f1-attempt-admission-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
try {
    $Contract=Join-Path $scratch 'invocation-contract.json'
    $Authorization=Join-Path $scratch 'attempt-authorization.json'
    $precheckOut=Join-Path $scratch 'out.txt'; $precheckErr=Join-Path $scratch 'err.txt'
    $Rootfs=$UbuntuSums=$UbuntuSignature=$OpenFoamKeyring=$NvidiaKeyring=$UbuntuCloudimageKeyring=$Contract
    $body = Get-Content (Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\run-1\invocation-contract.json') -Raw | ConvertFrom-Json
    foreach ($p in $data.psobject.Properties) { $body | Add-Member NoteProperty $p.Name $p.Value -Force }
    $body.inputs.psobject.Properties.Remove('b2_lock')   # the committed run-1 contract is /1; the B2 lock left the input set at C3
    $body.inputs | Add-Member NoteProperty ubuntu_cloudimage_keyring ([pscustomobject]@{ path='C:\reviewed\ubuntu-cloudimage-keyring.gpg'; sha256='c2d40d925557dbe9a0745c12ca0dc98bed593ce9a6652f1b2faa1ff0858dbf2f' }) -Force   # joined at C4
    $body | Add-Member NoteProperty target_path (Get-DerivedTargetPath $scratch) -Force   # Ruling 237: the admission span derives from $scratch
    $body | Add-Member NoteProperty external_sha256 @{ 'docs/proof/win-gpu-f1-preflight/recovery/check_cache.py'=(Get-FileHash (Join-Path $packet '..\recovery\check_cache.py')).Hash.ToLowerInvariant() }
    $body | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $Contract -Encoding UTF8
    # Override only filesystem names needed by this real admission span.
    $RepoRoot=$scratch; $RunRoot=Join-Path $scratch 'docs\proof\win-gpu-f1-preflight\attempt-1'
    New-Item -ItemType Directory -Path $RunRoot -Force | Out-Null
    $canonical=Join-Path $RunRoot 'invocation-contract.json'; Copy-Item $Contract $canonical; $Contract=$canonical
    foreach ($mode in @('missing','proposed','altered')) {
        $script:admissionChildren=0
        & pyOriginal -3 (Join-Path $PSScriptRoot 'f1_attempt_authority_probe.py') $Contract $Authorization $PacketRoot $mode
        $blocked=$false
        try { & $admission; Invoke-Child 'wsl.exe' @('--list') } catch {
            $blocked=$true
            Require ($_.Exception.Message -notlike '*WSL-SENTINEL*') 'invalid authority reached WSL boundary'
            $expected = if ($mode -eq 'missing') { '*authorization missing*' } elseif ($mode -eq 'proposed') { '*proposed authorization*' } else { '*source/run/hash/scope*' }
            Require ($_.Exception.Message -like $expected) ('wrong admission refusal for ' + $mode)
        }
        Require ($blocked -and $script:wslCalls -eq 0) 'invalid authority admitted a WSL call'
        Require ($script:admissionChildren -eq $(if ($mode -eq 'missing') { 0 } else { 1 })) 'authority checker was not reached as expected'
    }
} finally {
    $resolvedScratch = [IO.Path]::GetFullPath($scratch)
    if (-not $resolvedScratch.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase) -or
        -not ([IO.Path]::GetFileName($resolvedScratch)).StartsWith('f1-attempt-admission-')) { throw 'scratch cleanup left intended temp root' }
    Remove-Item -LiteralPath $resolvedScratch -Recurse -Force
}
Write-Output 'attempt admission PowerShell controls: PASS ring=offline-admission wsl_launches=0'
