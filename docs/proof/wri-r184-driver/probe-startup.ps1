# Ring: preparation diagnostic; four isolated stub probes, no scale/product/verifier. Ceiling: 10 s.
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../../../tools/windows-runner.ps1')
$scratch=Join-Path ([IO.Path]::GetTempPath()) ('cfd-startup-probe-'+[guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($scratch)
try {
    [void][IO.Directory]::CreateDirectory((Join-Path $scratch 'src'))
    foreach ($path in @('src/fixture.txt','global.json','CFDWorkbench.slnx')) { [IO.File]::WriteAllText((Join-Path $scratch $path),'baseline') }
    & git -C $scratch init -q
    & git -C $scratch add -A
    & git -C $scratch -c user.name=fixture -c user.email=fixture@example.invalid commit -q -m fixture
    if ($LASTEXITCODE -ne 0) { throw 'probe git failed' }
    $stub=Join-Path $scratch 'stub.ps1'
    [IO.File]::WriteAllText($stub,'Start-Sleep -Seconds 60')
    $engine=(Get-Process -Id $PID).Path
    foreach ($iteration in 1..4) {
        $clock=[Diagnostics.Stopwatch]::StartNew()
        $caught='NO EXCEPTION'
        try { $null=Invoke-WriChild -Repo $scratch -Exe $engine -Arguments @('-NoProfile','-File',$stub) -Clock $clock -CeilingMs 60000 -ChildCeilingMs 1000 -InjectStartupDelayMs 2000 }
        catch { $caught=$_.Exception.Message }
        "STARTUP-PROBE iteration=$iteration caught=$caught elapsed_ms=$($clock.ElapsedMilliseconds)"
    }
} finally {
    $resolved=[IO.Path]::GetFullPath($scratch)
    if (-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()),[StringComparison]::OrdinalIgnoreCase)) { throw 'scratch outside temporary directory' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
