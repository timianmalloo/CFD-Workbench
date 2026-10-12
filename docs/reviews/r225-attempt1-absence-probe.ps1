# Independent offline review probe. No WSL or registry API is invoked.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$packet = Join-Path $repo 'docs\proof\win-gpu-f1-preflight\packet'
. (Join-Path $packet 'Control.ps1')
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count -ne 0) { throw 'production entrypoint did not parse' }
$definition = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Assert-FinalAbsence' }, $true)
if ($null -eq $definition) { throw 'production absence check missing' }
Invoke-Expression $definition.Extent.Text
$Target = 'cfdw-petsc-cuda132-f1'
$Protected = 'cfdw-openfoam2512'
$TargetPath = 'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
$freshAttempt = $true
$script:CreatedTargetKey = '{11111111-2222-3333-4444-555555555555}'
$script:rows = @(
    [pscustomobject]@{DistributionName='renamed-survivor';PSChildName=$script:CreatedTargetKey;Version=2;BasePath='C:\Projects\moved-survivor';PSPath='fixture-target'},
    [pscustomobject]@{DistributionName=$Protected;PSChildName='{b7566a82-9cee-47ce-a246-f83632353cea}';Version=2;BasePath=[Environment]::ExpandEnvironmentVariables('%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512');PSPath='fixture-protected'}
)
function Get-DistroNames { return @($Protected, 'renamed-survivor') }
function Get-TargetRegistration { return @() }
function Test-Path { return $false }
function Get-ChildItem { return $script:rows }
function Get-ItemProperty { param($LiteralPath) return @($script:rows | Where-Object { $_.PSPath -eq $LiteralPath }) }
function Invoke-Child { throw 'WSL-SENTINEL: this probe must never launch a child' }
$refused = $false
try { Assert-FinalAbsence } catch { $refused = $true; Write-Output ('REFUSAL: ' + $_.Exception.Message) }
if (-not $refused) { throw 'REVIEW-FAIL: renamed/moved created target GUID survived but production Assert-FinalAbsence accepted' }
Write-Output 'REVIEW-PASS: surviving created target GUID refused; no live effects'
