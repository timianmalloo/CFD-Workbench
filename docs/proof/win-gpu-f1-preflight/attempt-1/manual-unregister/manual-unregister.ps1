# Ruling 232 (operator, 2026-10-13): remove the leftover F1 throwaway distro with plain commands, no cleanup tooling.
# Run once on the PC, in Windows PowerShell or pwsh, with no other WSL-touching session running:
#   powershell -NoProfile -ExecutionPolicy Bypass -File docs\proof\win-gpu-f1-preflight\attempt-1\manual-unregister\manual-unregister.ps1
# It aborts before any change if an identity differs, writes receipt.json beside itself, and prints the result.
# The protected distro's BasePath holds the user profile path, so the receipt records only whether it is unchanged.
$ErrorActionPreference = 'Stop'
$TargetName = 'cfdw-petsc-cuda132-f1'
$TargetKey = '{e874e10f-c7b9-4a2b-a03b-87ad52fb9c41}'
$TargetBase = 'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
$ProtectedName = 'cfdw-openfoam2512'
$ProtectedKey = '{b7566a82-9cee-47ce-a246-f83632353cea}'
$Receipt = Join-Path $PSScriptRoot 'receipt.json'

function Get-Lxss {
    Get-ChildItem 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Lxss' | ForEach-Object {
        $p = Get-ItemProperty $_.PSPath
        [pscustomobject]@{ Key = $_.PSChildName; Name = $p.DistributionName; BasePath = ($p.BasePath -replace '^\\\\\?\\', '') }
    }
}

function Public-View($rows) {
    @($rows | ForEach-Object {
        [ordered]@{ key = $_.Key; name = $_.Name; base_path = $(if ($_.Key -eq $TargetKey) { $_.BasePath } else { '<not recorded>' }) }
    })
}

function Write-Receipt($result, $before, $after, $protectedUnchanged) {
    $doc = [ordered]@{
        schema = 'cfdw-f1-manual-unregister/1'; ruling = 232; utc = (Get-Date).ToUniversalTime().ToString('o')
        result = $result; target = [ordered]@{ name = $TargetName; key = $TargetKey; base_path = $TargetBase }
        protected = [ordered]@{ name = $ProtectedName; key = $ProtectedKey; unchanged = $protectedUnchanged }
        before = (Public-View $before); after = (Public-View $after)
    }
    [IO.File]::WriteAllText($Receipt, (($doc | ConvertTo-Json -Depth 5) + "`n").Replace("`r`n", "`n"), (New-Object Text.UTF8Encoding($false)))
}

$before = @(Get-Lxss)
$t = @($before | Where-Object { $_.Key -eq $TargetKey })
$p = @($before | Where-Object { $_.Key -eq $ProtectedKey })
$sameName = @($before | Where-Object { $_.Name -eq $TargetName })
if ($t.Count -ne 1 -or $t[0].Name -ne $TargetName -or $t[0].BasePath -ne $TargetBase -or $sameName.Count -ne 1) {
    Write-Receipt 'ABORTED-TARGET-MISMATCH' $before $before $null; throw 'ABORT: target identity differs; nothing changed'
}
if ($p.Count -ne 1 -or $p[0].Name -ne $ProtectedName) {
    Write-Receipt 'ABORTED-PROTECTED-MISMATCH' $before $before $null; throw 'ABORT: protected identity differs; nothing changed'
}
$protectedBase = $p[0].BasePath

wsl.exe --terminate $TargetName
if ($LASTEXITCODE -ne 0) { Write-Receipt "FAILED-TERMINATE-$LASTEXITCODE" $before @(Get-Lxss) $null; throw "terminate exited $LASTEXITCODE" }
wsl.exe --unregister $TargetName
if ($LASTEXITCODE -ne 0) { Write-Receipt "FAILED-UNREGISTER-$LASTEXITCODE" $before @(Get-Lxss) $null; throw "unregister exited $LASTEXITCODE" }

$after = @(Get-Lxss)
$gone = @($after | Where-Object { $_.Key -eq $TargetKey -or $_.Name -eq $TargetName }).Count -eq 0
$p2 = @($after | Where-Object { $_.Key -eq $ProtectedKey })
$unchanged = ($p2.Count -eq 1 -and $p2[0].Name -eq $ProtectedName -and $p2[0].BasePath -eq $protectedBase)
if (-not $gone) { Write-Receipt 'FAILED-TARGET-STILL-REGISTERED' $before $after $unchanged; throw 'target still registered' }
if (-not $unchanged) { Write-Receipt 'FAILED-PROTECTED-CHANGED' $before $after $false; throw 'protected distro changed' }
Write-Receipt 'MANUAL-UNREGISTER-OK' $before $after $true
'MANUAL-UNREGISTER-OK'
