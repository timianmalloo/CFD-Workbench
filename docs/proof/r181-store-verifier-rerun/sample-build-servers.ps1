# Post-shutdown, in-memory classifier for the Ruling 181 SDK residual sample.
$ErrorActionPreference = 'Stop'
$clock = [Diagnostics.Stopwatch]::StartNew()
$sampleStartedUtc = [DateTime]::UtcNow
$processes = [Collections.Generic.List[object]]::new()
$unreadableCount = 0
$completion = 'complete'

try {
    # Candidate names are bounded by the retained R181 evidence. CommandLine is
    # read only to identify the SDK role and is never placed in an output object.
    $snapshot = @(Get-CimInstance -ClassName Win32_Process -ErrorAction Stop)
    foreach ($item in $snapshot) {
        if ($item.Name -notin @('dotnet.exe','MSBuild.exe','VBCSCompiler.exe')) { continue }
        $commandLine = [string]$item.CommandLine
        if (-not $commandLine) {
            $unreadableCount++
            continue
        }

        $role = $null
        if ($commandLine -match '(?i)\bMSBuild\.(?:dll|exe)\b') { $role = 'msbuild-server' }
        elseif ($commandLine -match '(?i)\bVBCSCompiler\.(?:dll|exe)\b') { $role = 'vbcs-compiler' }
        if ($null -eq $role) { continue }

        $createdUtc = $null
        try {
            if ($null -ne $item.CreationDate) {
                $createdUtc = ([DateTime]$item.CreationDate).ToUniversalTime().ToString('o')
            }
        } catch { $createdUtc = $null }
        if ($null -eq $createdUtc -or $null -eq $item.ProcessId -or $null -eq $item.ParentProcessId) {
            $unreadableCount++
        }
        $processes.Add([ordered]@{
            role = $role
            pid = if ($null -ne $item.ProcessId) { [uint32]$item.ProcessId } else { $null }
            parentPid = if ($null -ne $item.ParentProcessId) { [uint32]$item.ParentProcessId } else { $null }
            createdUtc = $createdUtc
        })
    }
} catch {
    $completion = 'failed'
    [Console]::Error.WriteLine('R181-RESIDUAL: process sample unavailable')
    exit 1
}

$clock.Stop()
if ($unreadableCount -gt 0) { $completion = 'partial-unreadable' }
$result = [ordered]@{
    sampleUtc = [DateTime]::UtcNow.ToString('o')
    elapsedMs = $clock.ElapsedMilliseconds
    completion = $completion
    count = $processes.Count
    unreadableCount = $unreadableCount
    processes = @($processes)
}
[Console]::Out.WriteLine(($result | ConvertTo-Json -Depth 5 -Compress))
