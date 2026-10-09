param([Parameter(Mandatory = $true)][string]$PythonPath)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$capture = Join-Path $PSScriptRoot 'capture'
$baseFiles = @{
    'build.stderr.txt' = '7eb70257593da06f682a3ddda54a9d260d4fc514f645237f5ca74b08f8da61a6'
    'build.stdout.txt' = 'ca70235801f32a60e84c328a77cf274960d90c38063b20ce8d85e719d1703b2d'
    'create-only.stderr.txt' = '7eb70257593da06f682a3ddda54a9d260d4fc514f645237f5ca74b08f8da61a6'
    'create-only.stdout.txt' = 'cc228a0d5a88306bdff7a095ab2ab43035dbb2870fef84a272011a29df8f61a3'
    'overwrite.stderr.txt' = '7eb70257593da06f682a3ddda54a9d260d4fc514f645237f5ca74b08f8da61a6'
    'overwrite.stdout.txt' = 'fe9aa8b4fc451f4cf7c2aee15eade50da70b86d75d0879d6295b3cf03ec2c601'
    'self-test.stderr.txt' = '7eb70257593da06f682a3ddda54a9d260d4fc514f645237f5ca74b08f8da61a6'
    'self-test.stdout.txt' = 'a53eeddeb2699f008e0d36dab1b7f2f74d613b0fe3cc13f4f227daebe99ab36e'
}
foreach ($name in $baseFiles.Keys) {
    $file = Join-Path $capture $name
    if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or
        (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() -cne $baseFiles[$name]) {
        throw 'COPY447-PACKAGE: retained initial capture bytes changed or missing'
    }
}

. (Join-Path $repo 'tools/windows-runner.ps1')
$env:CFD_PII_HOSTNAMES = [Environment]::MachineName
$clock = [Diagnostics.Stopwatch]::StartNew()
$deadlineMs = 90000
$runnerEvents = [Collections.Generic.List[string]]::new()
$result = Invoke-WriChild -Repo $repo -Exe $PythonPath `
    -Arguments @((Join-Path $repo 'tools/check-proof-pii.py')) `
    -Clock $clock -CeilingMs $deadlineMs -ChildCeilingMs 60000 `
    -PythonPath $PythonPath -Events $runnerEvents
$clock.Stop()

[IO.File]::WriteAllText((Join-Path $capture 'pii.stdout.txt'), $result.Stdout, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $capture 'pii.stderr.txt'), $result.Stderr, [Text.UTF8Encoding]::new($false))
$metadata = [ordered]@{
    schema = 'copy447-packaging-pii/v1'
    purpose = 'privacy check for retained partial evidence only; no probe or SaveAsync execution'
    command = @('py', '-3', 'tools/check-proof-pii.py')
    hostnameEnvironment = '%COMPUTERNAME%'
    numericExit = $result.ExitCode
    timedOut = $result.TimedOut
    wallMilliseconds = $clock.Elapsed.TotalMilliseconds
    childWallMilliseconds = $result.WallMs
    phnStatus = $result.PhnStatus
    rootExitDescendants = $result.RootExitDescendants
    retainedHandle = $result.RetainedHandle
    cleanup = $result.Cleanup
    residualStatus = $result.ResidualStatus
    rawStdoutSha256 = $result.RawStdoutSha256
    rawStderrSha256 = $result.RawStderrSha256
    runnerEvents = @($runnerEvents)
    outputSha256 = @{
        stdout = (Get-FileHash -LiteralPath (Join-Path $capture 'pii.stdout.txt') -Algorithm SHA256).Hash.ToLowerInvariant()
        stderr = (Get-FileHash -LiteralPath (Join-Path $capture 'pii.stderr.txt') -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$metaPath = Join-Path $capture 'pii-metadata.json'
[IO.File]::WriteAllText($metaPath, (($metadata | ConvertTo-Json -Depth 10) + "`n"), [Text.UTF8Encoding]::new($false))

$retained = @($baseFiles.Keys) + @('pii.stdout.txt', 'pii.stderr.txt', 'pii-metadata.json')
$rows = foreach ($name in ($retained | Sort-Object -CaseSensitive)) {
    $file = Join-Path $capture $name
    $bytes = [IO.File]::ReadAllBytes($file)
    [ordered]@{
        path = "docs/proof/copy447-reachability/capture/$name"
        bytes = $bytes.Length
        sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
    }
}
$manifest = [ordered]@{
    files = @($rows)
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'capture-manifest.json'), (($manifest | ConvertTo-Json -Depth 10) + "`n"), [Text.UTF8Encoding]::new($false))

if ($result.ExitCode -ne 0 -or $result.TimedOut -or $result.PhnStatus -ne 'PASS') {
    throw 'COPY447-PACKAGE: retained-file privacy check did not pass; manifest retains the observed numeric result'
}
if ($result.Stdout -notmatch 'PROOF-PII ok: 0 Windows user paths or machine SIDs') {
    throw 'COPY447-PACKAGE: privacy checker did not emit its zero-hit receipt'
}
"COPY447 PACKAGING_PII exit=$($result.ExitCode) timeout=$($result.TimedOut) PHN=$($result.PhnStatus) entries=$($rows.Count) wall_ms=$($clock.Elapsed.TotalMilliseconds)"
