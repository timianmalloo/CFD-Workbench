$ErrorActionPreference = 'Stop'
$helper = Join-Path $PSScriptRoot 'capture-calibration.ps1'
$report = Join-Path $PSScriptRoot 'calibration\deadline-self-test.txt'
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("cfd-capture-deadline-{0}" -f [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

function Invoke-HelperSelfTest([string] $ScriptPath) {
    $output = @(& pwsh -NoProfile -ExecutionPolicy Bypass -File $ScriptPath -Run 1 -SelfTest 2>&1)
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join [Environment]::NewLine) }
}

try {
    $source = Get-Content -LiteralPath $helper -Raw
    $normal = Invoke-HelperSelfTest -ScriptPath $helper
    if ($normal.ExitCode -ne 0) { throw "unmutated self-test failed: $($normal.Output)" }

    $waitNeedle = '$Process.WaitForExit([int]$remainingMs)'
    if (($source.Split($waitNeedle).Count - 1) -ne 1) { throw 'Expected one shared deadline-aware wait call.' }
    $waitMutant = Join-Path $tempDir 'wait-mutant.ps1'
    [System.IO.File]::WriteAllText($waitMutant, $source.Replace($waitNeedle, '$Process.WaitForExit(300000)'), [System.Text.UTF8Encoding]::new($false))
    $waitResult = Invoke-HelperSelfTest -ScriptPath $waitMutant
    if ($waitResult.ExitCode -eq 0 -or $waitResult.Output -notmatch 'Deadline wait self-test failed') {
        throw "hard-coded WaitForExit mutation was not rejected: $($waitResult.Output)"
    }

    $envelopeNeedle = '$outerEnvelopeExceeded = -not (Test-EnvelopeWithinCeiling -EnvelopeMs $utcEnvelopeMs -CeilingMs $outerCeilingMs)'
    if (($source.Split($envelopeNeedle).Count - 1) -ne 1) { throw 'Expected one total-envelope failure assignment.' }
    $envelopeMutant = Join-Path $tempDir 'envelope-mutant.ps1'
    [System.IO.File]::WriteAllText($envelopeMutant, $source.Replace($envelopeNeedle, '$outerEnvelopeExceeded = $false'), [System.Text.UTF8Encoding]::new($false))
    $envelopeResult = Invoke-HelperSelfTest -ScriptPath $envelopeMutant
    if ($envelopeResult.ExitCode -eq 0 -or $envelopeResult.Output -notmatch 'Total-envelope failure check is missing or bypassed') {
        throw "disabled total-envelope mutation was not rejected: $($envelopeResult.Output)"
    }

    @(
        'command=pwsh -NoProfile -ExecutionPolicy Bypass -File docs/proof/ring-windows/capture-calibration.ps1 -Run 1 -SelfTest'
        "normal_exit=$($normal.ExitCode)"
        $normal.Output
        'mutation=WaitForExit(remainingMs) -> WaitForExit(300000)'
        "wait_mutant_exit=$($waitResult.ExitCode)"
        'wait_mutation_rejected=Deadline wait self-test failed to pass the remaining budget.'
        'mutation=total-envelope result -> false'
        "envelope_mutant_exit=$($envelopeResult.ExitCode)"
        'envelope_mutation_rejected=Total-envelope failure check is missing or bypassed.'
        'verification=PASS both control mutations were rejected'
    ) | Set-Content -LiteralPath $report -Encoding utf8
    Write-Output 'capture deadline mutation verification PASS: hard-coded wait and disabled envelope controls were rejected'
} finally {
    if (Test-Path -LiteralPath $tempDir) { Remove-Item -LiteralPath $tempDir -Recurse -Force }
}
