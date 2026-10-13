param([string]$Case = 'all')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$source = Join-Path $repo 'docs\proof\win-gpu-f1-preflight\attempt-1\leader-renew.ps1'
$tokens=$null; $parseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile($source,[ref]$tokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw 'supervisor syntax invalid' }
if ($Case -eq 'all') {
    $engine = (Get-Process -Id $PID).Path
    foreach ($mode in @('valid','leader','epoch','state','read','renew','postread','postbinding','malformed')) {
        # Native stderr is evidence; PS5 must not throw before its exit is checked.
        $ErrorActionPreference='Continue'
        $output = & $engine -NoProfile -File $PSCommandPath -Case $mode 2>&1
        $exitCode = $LASTEXITCODE
        $ErrorActionPreference='Stop'
        $expected = if ($mode -eq 'valid') { 0 } else { 1 }
        if ($exitCode -ne $expected) { throw "wrong supervisor exit for $mode : $exitCode" }
        $text = $output -join "`n"
        if ($mode -eq 'valid' -and ($text -notmatch 'event=renew epoch=8 expires_in=899 oid=fixture-ref' -or $text -notmatch 'SLEEP=90')) {
            throw 'renewal event or default interval was not observed'
        }
        if ($mode -in @('leader','epoch','state','read','malformed') -and $text -match 'RENEW-CALLED') {
            throw 'invalid pre-renew binding reached mutation boundary'
        }
        if ($mode -in @('renew','postread','postbinding') -and $text -notmatch 'refusal=') { throw 'explicit refusal event missing' }
        Write-Output "SUPERVISOR-PASS case=$mode exit=$exitCode"
    }
    exit 0
}
$script:stopped=$false
$script:reads=0
function Test-Path { param($LiteralPath) if ($LiteralPath -cne 'offline-stop') { throw 'unexpected path boundary' }; return $script:stopped }
function Add-Content { param($LiteralPath,$Value) if ($LiteralPath -cne 'offline-log') { throw 'unexpected log boundary' }; Write-Output $Value }
function Start-Sleep { param($Seconds) Write-Output "SLEEP=$Seconds"; $script:stopped=$true }
function py {
    $global:LASTEXITCODE=0
    if ($args -contains 'who') {
        $script:reads++
        if (($Case -eq 'read' -and $script:reads -eq 1) -or ($Case -eq 'postread' -and $script:reads -eq 2)) {
            $global:LASTEXITCODE=5; return 'read failed'
        }
        if ($Case -eq 'malformed') { return 'not-json' }
        $value=@{leader='mac';epoch=8;state='live';expires_in=899.9;oid='fixture-ref'}
        if ($Case -eq 'leader') { $value.leader='other' }
        if ($Case -eq 'epoch' -or ($Case -eq 'postbinding' -and $script:reads -eq 2)) { $value.epoch=7 }
        if ($Case -eq 'state') { $value.state='expired' }
        return ($value | ConvertTo-Json -Compress)
    }
    if ($args -contains 'renew') {
        Write-Host 'RENEW-CALLED'
        if ($Case -eq 'renew') { $global:LASTEXITCODE=5; return 'renew failed' }
        return 'renewed'
    }
    throw 'unexpected command boundary; no real child may launch'
}
function wsl { throw 'WSL-SENTINEL' }
. $source -Repo $repo -StopFile 'offline-stop' -LogFile 'offline-log'
exit $LASTEXITCODE
