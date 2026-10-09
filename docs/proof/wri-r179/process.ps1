param([string]$Label,[string]$Exe,[string[]]$Arguments,[string]$Only='',[int]$TimeoutSeconds=60,[switch]$Affinity)
$ErrorActionPreference='Stop'
$proof=Join-Path (Get-Location) 'docs/proof/wri-r179'
$info=[System.Diagnostics.ProcessStartInfo]::new()
$info.FileName=$Exe
$info.Arguments=[string]::Join(' ',@($Arguments|ForEach-Object {'"'+$_.Replace('"','\"')+'"'}))
$info.WorkingDirectory=(Get-Location).Path
$info.UseShellExecute=$false;$info.CreateNoWindow=$true
$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
if($Only){$info.EnvironmentVariables['CFD_TEST_ONLY']=$Only}else{$info.EnvironmentVariables.Remove('CFD_TEST_ONLY')}
[IO.File]::WriteAllText((Join-Path $proof "$Label.command.txt"),"cwd=$($info.WorkingDirectory)`nexe=$Exe`narguments=$($info.Arguments)`nCFD_TEST_ONLY=$Only`naffinity_requested=$Affinity`n",[Text.UTF8Encoding]::new($false))
$p=[System.Diagnostics.Process]::new();$p.StartInfo=$info
$start=[DateTime]::UtcNow
$p.Start()|Out-Null
$childId=$p.Id;$affinityActual='not requested'
if($Affinity){try{$p.ProcessorAffinity=[IntPtr]0x3F;$affinityActual=$p.ProcessorAffinity.ToInt64().ToString('X')}catch{$affinityActual='unavailable: '+$_.Exception.Message}}
$out=[IO.File]::Create((Join-Path $proof "$Label.stdout.txt"))
$err=[IO.File]::Create((Join-Path $proof "$Label.stderr.txt"))
$ot=$p.StandardOutput.BaseStream.CopyToAsync($out);$et=$p.StandardError.BaseStream.CopyToAsync($err)
$timedOut=!$p.WaitForExit($TimeoutSeconds*1000)
if($timedOut){$p.Kill();$p.WaitForExit()}
$ot.GetAwaiter().GetResult();$et.GetAwaiter().GetResult();$out.Dispose();$err.Dispose()
$exit=[int]$p.ExitCode;$end=[DateTime]::UtcNow
$meta=[ordered]@{utc_start=$start.ToString('o');utc_end=$end.ToString('o');pid=$childId;duration_seconds=($end-$start).TotalSeconds;ProcessExitCode=$exit;timed_out=$timedOut;affinity=$affinityActual}
[IO.File]::WriteAllText((Join-Path $proof "$Label.measurement.json"),($meta|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
"PROCESS $Label PID=$childId Process.ExitCode=$exit seconds=$($meta.duration_seconds) timed_out=$timedOut affinity=$affinityActual"
Get-Content (Join-Path $proof "$Label.stdout.txt")
Get-Content (Join-Path $proof "$Label.stderr.txt")
