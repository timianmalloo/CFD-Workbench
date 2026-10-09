$ErrorActionPreference='Stop'
$repo='C:\Projects\CFD-Workbench-win-store-final-rerun-r175'
$proof=Join-Path $repo 'docs/proof/win-store-implementation/final-rerun-r175'
New-Item -ItemType Directory -Force $proof | Out-Null
Set-Location -LiteralPath $repo
$env:DOTNET_ROOT=[Environment]::GetEnvironmentVariable('DOTNET_ROOT','User')
$env:PATH=$env:DOTNET_ROOT+';'+[Environment]::GetEnvironmentVariable('PATH','User')+';'+[Environment]::GetEnvironmentVariable('PATH','Machine')
$env:DOTNET_PROCESSOR_COUNT='6'
$watchdogSelf=[Diagnostics.Process]::GetCurrentProcess()
$originalMask=$watchdogSelf.ProcessorAffinity.ToInt64()
$watchdogSelf.ProcessorAffinity=[IntPtr]0x3f
$sdkVersion=(& dotnet --version).Trim()
if($sdkVersion -ne '10.0.203'){throw "Pinned SDK unavailable: $sdkVersion"}
$utc=[DateTime]::UtcNow.ToString('o')
$watch=[Diagnostics.Stopwatch]::StartNew()
$child=Start-Process -FilePath (Get-Command py).Source -ArgumentList @('-3','tools/verify-windows-store.py') -WorkingDirectory $repo -WindowStyle Hidden -PassThru -RedirectStandardOutput "$proof/verifier.stdout.txt" -RedirectStandardError "$proof/verifier.stderr.txt"
$rootStart=$child.StartTime.ToUniversalTime().ToString('o')
$seen=@{}
$timedOut=$false
$taskkillExit=$null
while(-not $child.HasExited){
    $processes=@(Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId,CreationDate,Name,CommandLine)
    $tree=@([uint32]$child.Id)
    do {
        $new=@($processes | Where-Object {$_.ParentProcessId -in $tree -and $_.ProcessId -notin $tree})
        $tree+=@($new | ForEach-Object {$_.ProcessId})
    } while($new.Count -gt 0)
    foreach($item in $processes | Where-Object {$_.ProcessId -in $tree}){
        $key="$($item.ProcessId)|$($item.CreationDate.ToUniversalTime().Ticks)"
        if(-not $seen.ContainsKey($key)){
            $seen[$key]=@{pid=$item.ProcessId;parentPid=$item.ParentProcessId;createdUtc=$item.CreationDate.ToUniversalTime().ToString('o');name=$item.Name;commandLine=($item.CommandLine -replace [regex]::Escape($env:USERPROFILE),'%USERPROFILE%')}
        }
    }
    if($watch.Elapsed.TotalSeconds -ge 60){
        $timedOut=$true
        & taskkill.exe /PID $child.Id /T /F *> "$proof/watchdog-taskkill.txt"
        $taskkillExit=$LASTEXITCODE
        $child.WaitForExit(2000) | Out-Null
        break
    }
    $child.WaitForExit(100) | Out-Null
    $child.Refresh()
}
$watch.Stop()
$child.Refresh()
$exit=if($child.HasExited){$child.ExitCode}else{$null}
$residual=@()
foreach($item in $seen.Values){
    $current=Get-CimInstance Win32_Process -Filter "ProcessId=$($item.pid)" -ErrorAction SilentlyContinue
    if($current -and $current.CreationDate.ToUniversalTime().ToString('o') -eq $item.createdUtc){$residual+=$item}
}
$metadata=@{testedHead=(git rev-parse HEAD);scriptBlob=(git hash-object tools/verify-windows-store.py);scriptSha256=(Get-FileHash tools/verify-windows-store.py).Hash.ToLowerInvariant();command='py -3 tools/verify-windows-store.py';startUtc=$utc;endUtc=[DateTime]::UtcNow.ToString('o');launcherPid=$child.Id;launcherStartUtc=$rootStart;watchdogPid=$PID;watchdogOriginalAffinity=('0x{0:x}' -f $originalMask);requestedAffinity='0x3f';sdkVersion=$sdkVersion;dotnetRoot='%USERPROFILE%\.dotnet';dotnetExecutable=('%USERPROFILE%\.dotnet\dotnet.exe');osVersion=[Environment]::OSVersion.VersionString;outerWallSeconds=$watch.Elapsed.TotalSeconds;watchdogLimitSeconds=60;watchdogTimedOut=$timedOut;processExited=$child.HasExited;processExit=$exit;taskkillExit=$taskkillExit;observedProcessTree=@($seen.Values | Sort-Object pid);observedResidualProcesses=$residual;unobservedDescendants='Not assessed: snapshot sampling can miss short-lived descendants';result=if(-not $timedOut -and $child.HasExited -and $exit -eq 0 -and $residual.Count -eq 0){'PASS'}else{'FAIL'}}
$metadata | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath "$proof/process.json" -Encoding utf8
$watchdogSelf.ProcessorAffinity=[IntPtr]$originalMask
$metadata | Select-Object testedHead,launcherPid,processExit,outerWallSeconds,watchdogTimedOut,result | Format-List
