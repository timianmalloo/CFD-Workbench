param([string]$SourceCommit = '')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=Split-Path -Parent $PSScriptRoot
$packet=Join-Path $repo 'docs\proof\win-gpu-f1-preflight\packet'
. (Join-Path $packet 'Control.ps1')
$text = if ($SourceCommit) { (& git -C $repo show ($SourceCommit + ':docs/proof/win-gpu-f1-preflight/packet/Invoke-Preflight.ps1')) -join "`n" } else { [IO.File]::ReadAllText((Join-Path $packet 'Invoke-Preflight.ps1')) }
$tokens=$null; $errors=$null
$ast=[Management.Automation.Language.Parser]::ParseInput($text,[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'wrapper syntax failed' }
$definition=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Assert-FinalAbsence'},$true)
Invoke-Expression $definition.Extent.Text
$Target='cfdw-petsc-cuda132-f1'; $Protected='cfdw-openfoam2512'
$TargetPath='C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'; $freshAttempt=$true
$attemptNumber=1 # Historical fixture retains the attempt-1 identity policy.
$script:CreatedTargetKey='{11111111-2222-3333-4444-555555555555}'
$protectedRow=[pscustomobject]@{DistributionName=$Protected;PSChildName='{b7566a82-9cee-47ce-a246-f83632353cea}';Version=2;BasePath=[Environment]::ExpandEnvironmentVariables('%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512');PSPath='protected'} # machine-path-ok: synthetic fixture expansion; no expanded value is captured.
$survivor=[pscustomobject]@{DistributionName='renamed-survivor';PSChildName=$script:CreatedTargetKey;Version=2;BasePath='C:\Projects\moved-survivor';PSPath='survivor'}
$script:rows=@($protectedRow,$survivor)
function Get-DistroNames { return @($Protected,'renamed-survivor') }
function Get-TargetRegistration { return @() }
function Test-Path { return $false }
function Get-ChildItem { return $script:rows }
function Get-ItemProperty {param($LiteralPath) return @($script:rows | Where-Object {$_.PSPath -eq $LiteralPath})}
function Invoke-Child { throw 'WSL-SENTINEL: forbidden' }
function RequireRefusal { $refused=$false; try { Assert-FinalAbsence } catch { $refused=$true }; if (-not $refused) { throw 'renamed/moved created GUID or changed protected identity accepted as absent' } }
RequireRefusal
$script:rows=@($protectedRow)
Assert-FinalAbsence
foreach ($field in @('DistributionName','PSChildName','BasePath','Version')) {
    $changed=$protectedRow.psobject.Copy()
    $changed.$field=if ($field -eq 'Version') { 1 } else { 'changed' }
    $script:rows=@($changed)
    RequireRefusal
}
Write-Output 'production final absence: PASS created-GUID and protected-identity mutations refused; wsl_launches=0'
