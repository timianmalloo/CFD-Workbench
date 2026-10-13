param(
    [Parameter(Mandatory = $true)][string]$Repo,
    [Parameter(Mandatory = $true)][string]$StopFile,
    [Parameter(Mandatory = $true)][string]$LogFile,
    [int]$ExpectedEpoch = 8,
    [int]$IntervalSeconds = 90
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Set-Location -LiteralPath $Repo
$env:AGENT_SESSION = 'mac'
$env:AGENT_HOST = 'codex-windows'
$coord = 'docs/ai-forward-pack/scripts/coord-core.py'

while (-not (Test-Path -LiteralPath $StopFile)) {
    $beforeRaw = & py -3 $coord leader who --json 2>&1
    if ($LASTEXITCODE -ne 0) {
        Add-Content -LiteralPath $LogFile -Value ("$((Get-Date).ToUniversalTime().ToString('o')) refusal=leader-read $($beforeRaw -join ' | ')")
        exit 1
    }
    $before = $beforeRaw | ConvertFrom-Json
    if ($before.state -cne 'live' -or $before.leader -cne 'mac' -or $before.epoch -ne $ExpectedEpoch) {
        Add-Content -LiteralPath $LogFile -Value ("$((Get-Date).ToUniversalTime().ToString('o')) refusal=leader-binding observed=$($beforeRaw -join '')")
        exit 1
    }

    $renewRaw = & py -3 $coord leader renew 2>&1
    if ($LASTEXITCODE -ne 0) {
        Add-Content -LiteralPath $LogFile -Value ("$((Get-Date).ToUniversalTime().ToString('o')) refusal=renew $($renewRaw -join ' | ')")
        exit 1
    }
    $afterRaw = & py -3 $coord leader who --json 2>&1
    if ($LASTEXITCODE -ne 0) {
        Add-Content -LiteralPath $LogFile -Value ("$((Get-Date).ToUniversalTime().ToString('o')) refusal=post-renew-read $($afterRaw -join ' | ')")
        exit 1
    }
    $after = $afterRaw | ConvertFrom-Json
    if ($after.state -cne 'live' -or $after.leader -cne 'mac' -or $after.epoch -ne $ExpectedEpoch) {
        Add-Content -LiteralPath $LogFile -Value ("$((Get-Date).ToUniversalTime().ToString('o')) refusal=post-renew-binding observed=$($afterRaw -join '')")
        exit 1
    }
    Add-Content -LiteralPath $LogFile -Value ("$((Get-Date).ToUniversalTime().ToString('o')) event=renew epoch=$($after.epoch) expires_in=$([Math]::Floor($after.expires_in)) oid=$($after.oid)")
    Start-Sleep -Seconds $IntervalSeconds
}

exit 0
