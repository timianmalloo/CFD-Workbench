# F1 re-scope node C2 (Rulings 237, 238 (3), 240): machine paths derive from the repository location, never a constant;
# the drive letter follows the target; the free-space floors come from the contract. Offline-fast; both runtimes; no
# WSL effect. The fixture paths are literals of this test, never of the packet.
$ErrorActionPreference = 'Stop'; Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
$packet = Join-Path $root 'docs\proof\win-gpu-f1-preflight\packet'
. (Join-Path $packet 'Control.ps1')
$errors = $null; $tokens = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'launcher parse failed' }
foreach ($name in @('Convert-LinuxPath', 'Assert-TargetPath', 'Get-HostFree')) {
    $node = $ast.Find({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name }, $true)
    if ($null -eq $node) { throw "launcher function missing: $name" }
    Invoke-Expression $node.Extent.Text
}
function Refuses([scriptblock]$Block, [string]$Label) {
    $failed = $false
    try { $null = & $Block } catch { $failed = $true }
    if (-not $failed) { throw "${Label}: admitted" }
}
$oldConstant = 'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'

# TargetPath_RepoOnD_DerivesD
$derivedD = Get-DerivedTargetPath 'D:\Projects\CFD-Workbench-f1-attempt2'
if ($derivedD -cne 'D:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1') { throw "TargetPath_RepoOnD_DerivesD: $derivedD" }
if ((Get-DerivedTargetPath 'D:\Projects\CFD-Workbench-f1-attempt2\') -cne $derivedD) { throw 'TargetPath_RepoOnD_DerivesD: trailing separator changed the result' }
# TargetPath_RepoOnC_DerivesOldConstant
$derivedC = Get-DerivedTargetPath 'C:\Projects\CFD-Workbench-win-gpu-f1-r225-attempt1'
if ($derivedC -cne $oldConstant) { throw "TargetPath_RepoOnC_DerivesOldConstant: $derivedC" }
Refuses { Get-DerivedTargetPath 'D:\' } 'TargetPath: a drive root has no parent'
Refuses { Get-DerivedTargetPath 'D:\Pro jects\CFD-Workbench' } 'TargetPath: whitespace'
Refuses { Get-DerivedTargetPath '\\server\share\CFD-Workbench' } 'TargetPath: UNC'
Refuses { Get-DerivedTargetPath 'relative\CFD-Workbench' } 'TargetPath: relative'

# TargetPath_ContractDiffersFromDerived_Refuses (Assert-TargetPath reads $RepoRoot, $TargetPath and $contractData)
$RepoRoot = 'D:\Projects\CFD-Workbench-f1-attempt2'
$TargetPath = $derivedD
$contractData = [pscustomobject]@{ target_path = $oldConstant }
Refuses { Assert-TargetPath } 'TargetPath_ContractDiffersFromDerived_Refuses'
$contractData = [pscustomobject]@{ target_path = $derivedD }
if ((Assert-TargetPath) -cne $derivedD) { throw 'Assert-TargetPath: derived and recorded agree but it did not return the path' }
$contractData = [pscustomobject]@{ target_path = $derivedD.ToUpperInvariant() }
if ((Assert-TargetPath) -cne $derivedD) { throw 'Assert-TargetPath: case must not matter on NTFS' }
$TargetPath = $oldConstant
$contractData = [pscustomobject]@{ target_path = $oldConstant }
Refuses { Assert-TargetPath } 'Assert-TargetPath: a bound path that is not the derived one'
$TargetPath = $derivedD

# The Python side derives the same path (one rule, two surfaces, proved equal): contract.py target-path --repo
$out = Join-Path ([IO.Path]::GetTempPath()) ('cfdw-f1-c2-' + [Guid]::NewGuid().ToString('n') + '.txt')
$err = "$out.err"
try {
    $process = Start-Process -FilePath 'py' -ArgumentList @('-3', (Join-Path $packet 'contract.py'), 'target-path', '--repo', $RepoRoot) `
        -PassThru -NoNewWindow -RedirectStandardOutput $out -RedirectStandardError $err
    $null = $process.Handle
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "contract.py target-path exited $($process.ExitCode): $([IO.File]::ReadAllText($err))" }
    $python = [IO.File]::ReadAllText($out).Trim()
    if ($python -cne $derivedD) { throw "Python derives '$python', PowerShell derives '$derivedD'" }
} finally { Remove-Item -LiteralPath $out, $err -Force -ErrorAction SilentlyContinue }

# LinuxPath_DriveD_MntD
if ((Convert-LinuxPath 'D:\Projects\x\y.txt') -cne '/mnt/d/Projects/x/y.txt') { throw 'LinuxPath_DriveD_MntD' }
if ((Convert-LinuxPath 'C:\Projects\x') -cne '/mnt/c/Projects/x') { throw 'LinuxPath: C: still maps to /mnt/c' }
if ((Convert-LinuxPath 'e:\x') -cne '/mnt/e/x') { throw 'LinuxPath: the drive letter is lower-cased' }
Refuses { Convert-LinuxPath 'D:\a b\c' } 'LinuxPath: whitespace'
Refuses { Convert-LinuxPath '\\server\share\x' } 'LinuxPath: UNC'

# Get-HostFree measures the target's drive, not C:
$script:askedDrive = ''
function Get-PSDrive { param($Name) $script:askedDrive = $Name; return [pscustomobject]@{ Free = [int64]5 } }
$TargetPath = $derivedD
if ((Get-HostFree) -ne 5 -or $script:askedDrive -cne 'D') { throw "Get-HostFree asked drive '$($script:askedDrive)' for a D: target" }
$TargetPath = $oldConstant
$null = Get-HostFree
if ($script:askedDrive -cne 'C') { throw 'Get-HostFree asked the wrong drive for a C: target' }

# HostFloor_ContractWithoutFloor_Refuses
Refuses { Get-ContractHostFloors ([pscustomobject]@{ target_path = $derivedD }) } 'HostFloor_ContractWithoutFloor_Refuses'
Refuses { Get-ContractHostFloors ([pscustomobject]@{ host_start_floor_bytes = 19684091961 }) } 'HostFloor: reserve absent'
Refuses { Get-ContractHostFloors ([pscustomobject]@{ host_start_floor_bytes = '19684091961'; host_reserve_bytes = 10737418240 }) } 'HostFloor: a string'
Refuses { Get-ContractHostFloors ([pscustomobject]@{ host_start_floor_bytes = 0; host_reserve_bytes = 10737418240 }) } 'HostFloor: zero'
Refuses { Get-ContractHostFloors ([pscustomobject]@{ host_start_floor_bytes = 10737418240; host_reserve_bytes = 10737418240 }) } 'HostFloor: floor not above reserve'
Refuses { Get-ContractHostFloors ([pscustomobject]@{ host_start_floor_bytes = $true; host_reserve_bytes = 1 }) } 'HostFloor: a boolean'
$floors = Get-ContractHostFloors ([pscustomobject]@{ host_start_floor_bytes = 19684091961; host_reserve_bytes = 10737418240 })
if ($floors.Count -ne 2 -or $floors[0] -ne 19684091961 -or $floors[1] -ne 10737418240 -or $floors[0] -isnot [int64]) { throw 'HostFloor: the pair was not returned as int64' }
Write-Output "f1 derived paths PASS runtime=$($PSVersionTable.PSVersion); target path derives from the repo parent on D: and C:, a contract that differs refuses, /mnt/<drive>, host free on the target's drive, floors only from the contract"
