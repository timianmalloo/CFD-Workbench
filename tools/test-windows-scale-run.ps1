# Ring: runner preparation; pure callbacks, no scale/product/verifier. Ceiling: 10 s.
param([string]$DriverPath=(Join-Path $PSScriptRoot 'windows-scale-run.ps1'))
$ErrorActionPreference='Stop'
. $DriverPath
$events=[Collections.Generic.List[string]]::new()
$caught=$null
try {
    Invoke-WriScaleSequence -Preflight { param($expected); [void]$events.Add("preflight:$expected") } `
        -Build { [void]$events.Add('build') } `
        -Select { param($scale); [void]$events.Add("select:$scale") } `
        -Contract { param($scale); [void]$events.Add("checks:$scale"); if ($scale -eq 200) { throw 'stub-contract-failure' } } `
        -Restore { [void]$events.Add('restore:150:exit0') } `
        -Readback { [void]$events.Add('readback:1.5/1.5:exit0') }
} catch { $caught=$_.Exception.Message }
$expected=@('preflight:150% (Recommended)','build','select:150','checks:150','select:200','checks:200','restore:150:exit0','readback:1.5/1.5:exit0')
if ($caught -ne 'stub-contract-failure' -or ($events -join '|') -cne ($expected -join '|')) { throw "RESTORE-CONTRACT FAIL events=$($events -join '>')" }
"GREEN restore-on-throw events=$($events -join '>') primary_failure_preserved=true"
$events.Clear()
$caught=$null
try {
    Invoke-WriScaleSequence -Preflight {} -Build {} -Select {} -Contract {} `
        -Restore { [void]$events.Add('restore:failed'); throw 'stub-restore-failure' } `
        -Readback { [void]$events.Add('readback:attempted') }
} catch { $caught=$_.Exception.Message }
if ($caught -ne 'stub-restore-failure' -or ($events -join '|') -cne 'restore:failed|readback:attempted') { throw 'RESTORE-CONTRACT FAIL readback not attempted on restore failure' }
'GREEN readback-after-restore-throw attempted=true failure_preserved=true'
$events.Clear()
try {
    Invoke-WriScaleSequence -Preflight { throw 'stub-preflight-failure' } -Build {} `
        -Select { [void]$events.Add('select') } -Contract {} `
        -Restore { [void]$events.Add('restore') } -Readback { [void]$events.Add('readback') }
} catch { if ($_.Exception.Message -ne 'stub-preflight-failure') { throw } }
if ($events.Count) { throw 'RESTORE-CONTRACT FAIL preflight rejection mutated scale' }
'GREEN preflight-rejected scale_actions=0'
# Exercise the driver's actual preflight callback; every external child and source/toolchain probe is stubbed.
$script:boundaryFiles=[Collections.Generic.List[string]]::new()
function Assert-WriSourceClean {}
function Assert-WriSourceUnchanged {}
function Get-WriSourceFingerprint { return 'boundary-stub' }
function Assert-WriToolchain { return [pscustomobject]@{Dotnet='boundary-stub';Python='boundary-stub'} }
function Invoke-WriScaleRecordedChild {
    param($Context,$Label,$Exe,[string[]]$Arguments,$Only,$DeadlineMs,$ChildCeilingMs,$Mode)
    if ($Label -eq 'preflight-initial') { [void]$script:boundaryFiles.Add($Arguments[2]) }
    if ($Label -eq 'build') { throw 'boundary-stop-before-build' }
    return [pscustomobject]@{ExitCode=0;Stdout='SCALE_CONTEXT mode=scale-diagnostic RenderScaling=1.5 PrimaryScaling=1.5 boundary=stub'}
}
function Invoke-WriProcess {
    param($Repo,$Exe,$Arguments,$Clock,$CeilingMs)
    return [pscustomobject]@{ExitCode=0;Stdout=([IO.File]::ReadAllText($Arguments[2]))}
}
$boundaryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../docs/proof/wri-r184-driver'))+[IO.Path]::DirectorySeparatorChar
$boundaryOutput=Join-Path $boundaryRoot ('callback-boundary-'+[guid]::NewGuid().ToString('N'))
try {
    $caught=$null
    try { Invoke-WriScaleRun -PythonPath 'boundary-stub' -OutputDirectory $boundaryOutput }
    catch { $caught=$_.Exception.Message }
    $expectedPath=Join-Path (Split-Path -Parent ([IO.Path]::GetFullPath($DriverPath))) 'windows-settings-preflight.ps1'
    if ($caught -notlike 'WRI-RUN:*' -or $script:boundaryFiles.Count -ne 1 -or $script:boundaryFiles[0] -cne $expectedPath) { throw 'WRI-BOUNDARY: actual preflight -File must receive the script path, not callback text' }
    'GREEN preflight-file-boundary actual_driver_callback=true script_path=true stub_children=true'
} finally {
    $resolved=[IO.Path]::GetFullPath($boundaryOutput)
    if (-not $resolved.StartsWith($boundaryRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'boundary cleanup outside owned proof root' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
'SELFTEST PASS driver callbacks; no scale/product/verifier execution'
