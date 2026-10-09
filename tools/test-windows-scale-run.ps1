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
'SELFTEST PASS driver callbacks; no scale/product/verifier execution'
