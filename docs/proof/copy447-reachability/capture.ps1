param([Parameter(Mandatory = $true)][string]$PythonPath)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$proof = $PSScriptRoot
$captureDir = Join-Path $proof 'capture'
$artifactRoot = Join-Path ([IO.Path]::GetTempPath()) ('copy447-build-' + [guid]::NewGuid().ToString('N'))
if (Test-Path -LiteralPath $captureDir) { throw 'COPY447-CAPTURE: capture directory already exists; refusing overwrite' }
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
function Assert-Copy447TempChild([string]$Path, [string]$Prefix) {
    $resolved = [IO.Path]::GetFullPath($Path)
    $parent = [IO.Path]::GetDirectoryName($resolved).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $leaf = [IO.Path]::GetFileName($resolved)
    if (-not $parent.Equals($script:tempRoot, [StringComparison]::OrdinalIgnoreCase) -or -not $leaf.StartsWith($Prefix, [StringComparison]::Ordinal)) {
        throw 'COPY447-PATH: recursive cleanup target failed direct-child and prefix checks'
    }
}
Assert-Copy447TempChild $artifactRoot 'copy447-build-'
[void][IO.Directory]::CreateDirectory($captureDir)
[void][IO.Directory]::CreateDirectory($artifactRoot)
try {
. (Join-Path $repo 'tools/windows-runner.ps1')
$toolchain = Assert-WriToolchain -Repo $repo -PythonPath $PythonPath
$clock = [Diagnostics.Stopwatch]::StartNew()
$ceilingMs = 300000
$events = [Collections.Generic.List[string]]::new()
$project = Join-Path $proof 'Probe.csproj'
$buildArgs = @('build', $project, '--configuration', 'Release', '--artifacts-path', (Join-Path $artifactRoot 'artifacts'), '--disable-build-servers', '-nologo', '-v', 'q', '-p:NuGetAudit=false', '-p:UseSharedCompilation=false')
$build = Invoke-WriChild -Repo $repo -Exe $toolchain.Dotnet -Arguments $buildArgs -Clock $clock -CeilingMs $ceilingMs -ChildCeilingMs 120000 -Mode BuildVerifier -Toolchain $toolchain -Events $events
$build.stdout | Out-File -LiteralPath (Join-Path $captureDir 'build.stdout.txt') -Encoding utf8NoBOM
$build.stderr | Out-File -LiteralPath (Join-Path $captureDir 'build.stderr.txt') -Encoding utf8NoBOM
if ($build.ExitCode -ne 0 -or $build.TimedOut -or $build.PhnStatus -ne 'PASS') { throw "COPY447-BUILD: rejected numeric_exit=$($build.ExitCode) timeout=$($build.TimedOut) PHN=$($build.PhnStatus)" }
$dll = Join-Path $artifactRoot 'artifacts/bin/Probe/release/Probe.dll'
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw 'COPY447-BUILD: expected Probe.dll missing from artifacts path' }
$selfArgs = @('exec', $dll, '--self-test')
$self = Invoke-WriChild -Repo $repo -Exe $toolchain.Dotnet -Arguments $selfArgs -Clock $clock -CeilingMs $ceilingMs -ChildCeilingMs 60000 -Mode BuildVerifier -Toolchain $toolchain -Events $events
$self.stdout | Out-File -LiteralPath (Join-Path $captureDir 'self-test.stdout.txt') -Encoding utf8NoBOM
$self.stderr | Out-File -LiteralPath (Join-Path $captureDir 'self-test.stderr.txt') -Encoding utf8NoBOM
if ($self.ExitCode -ne 0 -or $self.TimedOut -or $self.PhnStatus -ne 'PASS' -or $self.stdout -notmatch 'SELFTEST PASS inventory') { throw "COPY447-SELFTEST: rejected numeric_exit=$($self.ExitCode) timeout=$($self.TimedOut) PHN=$($self.PhnStatus)" }

$cases = @(
    @{ Name = 'create-only'; RootName = 'copy447-create-only-' + [guid]::NewGuid().ToString('N') },
    @{ Name = 'overwrite'; RootName = 'copy447-overwrite-' + [guid]::NewGuid().ToString('N') }
)
$caseRecords = [Collections.Generic.List[object]]::new()
foreach ($case in $cases) {
    $root = Join-Path ([IO.Path]::GetTempPath()) $case.RootName
    $caseArgs = @('exec', $dll, '--case', $case.Name, $root)
    Assert-Copy447TempChild $root 'copy447-'
    try {
        $result = Invoke-WriChild -Repo $repo -Exe $toolchain.Dotnet -Arguments $caseArgs -Clock $clock -CeilingMs $ceilingMs -ChildCeilingMs 60000 -Mode BuildVerifier -Toolchain $toolchain -Events $events
        $result.stdout | Out-File -LiteralPath (Join-Path $captureDir ($case.Name + '.stdout.txt')) -Encoding utf8NoBOM
        $result.stderr | Out-File -LiteralPath (Join-Path $captureDir ($case.Name + '.stderr.txt')) -Encoding utf8NoBOM
        if ($result.ExitCode -ne 0 -or $result.TimedOut -or $result.PhnStatus -ne 'PASS') { throw "COPY447-CASE: $($case.Name) rejected numeric_exit=$($result.ExitCode) timeout=$($result.TimedOut) PHN=$($result.PhnStatus)" }
        $record = $result.stdout | ConvertFrom-Json
        $exactClosedAdmission = $record.saveResult.code -ceq 'DOC-UNSUPPORTED-PERSISTENCE' -and
            $null -eq $record.saveResult.publishedSha256 -and
            $record.saveResult.publicationKnown -eq $false -and
            $record.saveResult.durabilityConfirmed -eq $false
        $expectedInventory = if ($case.Name -eq 'create-only') { @() } else { @('project.cfdw') }
        $beforeNames = @($record.before | ForEach-Object { $_.relativeName })
        $afterNames = @($record.after | ForEach-Object { $_.relativeName })
        $inventoryUnchanged = (($beforeNames -join "`n") -ceq ($afterNames -join "`n"))
        $targetUnchanged = $record.target.unchangedFromBefore -eq $true
        $preconditionExact = (($beforeNames -join "`n") -ceq ($expectedInventory -join "`n"))
        if (-not $exactClosedAdmission -or -not $preconditionExact -or -not $inventoryUnchanged -or -not $targetUnchanged) {
            throw "COPY447-ASSERT: $($case.Name) did not match closed-admission and unchanged-inventory contract"
        }
        $caseRecords.Add([pscustomobject]@{
            variant = $case.Name
            command = @('%USERPROFILE%\.dotnet\dotnet.exe', 'exec', '%TEMP%\<copy447-build>\artifacts\bin\Probe\release\Probe.dll', '--case', $case.Name, '%TEMP%\<case-root>')
            numericExit = $result.ExitCode
            timedOut = $result.TimedOut
            wallMilliseconds = $result.WallMs
            buildServerShutdownExit = $result.BuildServerShutdownExit
            rootExitDescendants = $result.RootExitDescendants
            retainedHandle = $result.RetainedHandle
            residualStatus = $result.ResidualStatus
            cleanup = $result.Cleanup
            phnStatus = $result.PhnStatus
            rawStdoutSha256 = $result.RawStdoutSha256
            rawStderrSha256 = $result.RawStderrSha256
            substitutions = @($result.SubstitutionBinding)
            exactClosedAdmission = $exactClosedAdmission
            preconditionExact = $preconditionExact
            inventoryUnchanged = $inventoryUnchanged
            targetUnchanged = $targetUnchanged
            record = $record
        })
    } finally {
        Assert-Copy447TempChild $root 'copy447-'
        if (Test-Path -LiteralPath $root) {
            Remove-Item -LiteralPath $root -Recurse -Force
            if (Test-Path -LiteralPath $root) { throw 'COPY447-CLEANUP: isolated case root remains after recursive delete' }
        }
    }
}

$sourcePaths = @(
    'src/CfdWorkbench.Persistence/ProjectStore.cs',
    'src/CfdWorkbench.Persistence/WindowsNative.cs',
    'src/CfdWorkbench.Persistence/CfdWorkbench.Persistence.csproj',
    'src/CfdWorkbench.Core/AuthoringSession.cs',
    'global.json',
    'tools/windows-runner.ps1',
    'tools/check-windows-runner.py',
    'docs/proof/copy447-reachability/Probe.csproj',
    'docs/proof/copy447-reachability/Program.cs',
    'docs/proof/copy447-reachability/capture.ps1'
)
$sourceHashes = @{}
foreach ($path in $sourcePaths) {
    $sourceHashes[$path] = (Get-FileHash -LiteralPath (Join-Path $repo $path) -Algorithm SHA256).Hash.ToLowerInvariant()
}
$binaryFiles = Get-ChildItem -LiteralPath (Split-Path -Parent $dll) -File | Sort-Object Name
$binaryHashes = @{}
foreach ($file in $binaryFiles) { $binaryHashes[$file.Name] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
$gitHead = (& git -C $repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'COPY447-SOURCE: failed to read HEAD' }
$env:CFD_PII_HOSTNAMES = [Environment]::MachineName
$pii = Invoke-WriChild -Repo $repo -Exe $toolchain.Python -Arguments @((Join-Path $repo 'tools/check-proof-pii.py')) -Clock $clock -CeilingMs $ceilingMs -ChildCeilingMs 60000 -Events $events
$pii.stdout | Out-File -LiteralPath (Join-Path $captureDir 'pii.stdout.txt') -Encoding utf8NoBOM
$pii.stderr | Out-File -LiteralPath (Join-Path $captureDir 'pii.stderr.txt') -Encoding utf8NoBOM
if ($pii.ExitCode -ne 0 -or $pii.TimedOut -or $pii.PhnStatus -ne 'PASS') { throw 'COPY447-PII: tracked/untracked proof tree failed host/user-path PII check' }
$receipt = [ordered]@{
    schema = 'copy447-reachability-receipt/v1'
    head = $gitHead
    verdict = 'NOT ASSESSED for crash-left artifacts: public ProjectStore.SaveAsync on this Windows build refuses before filesystem stages.'
    commandContract = 'Pinned dotnet 10.0.203; BuildVerifier through committed tools/windows-runner.ps1; child exits, timeouts, build-server shutdowns, job cleanup and PHN status retained.'
    toolchain = @{
        sdk = $toolchain.Sdk
        dotnetPath = '%USERPROFILE%\\.dotnet\\dotnet.exe'
        dotnetExecutableSha256 = (Get-FileHash -LiteralPath $toolchain.Dotnet -Algorithm SHA256).Hash.ToLowerInvariant()
        pythonVersion = $toolchain.PythonVersion
        pythonPath = '%CFD_WRI_PYTHON%'
        pythonExecutableSha256 = (Get-FileHash -LiteralPath $toolchain.Python -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    sourceSha256 = $sourceHashes
    binarySha256 = $binaryHashes
    runnerEvents = @($events)
    build = @{ command = @('%USERPROFILE%\.dotnet\dotnet.exe', 'build', 'docs/proof/copy447-reachability/Probe.csproj', '--configuration', 'Release', '--artifacts-path', '%TEMP%\<copy447-build>\artifacts', '--disable-build-servers', '-nologo', '-v', 'q', '-p:NuGetAudit=false', '-p:UseSharedCompilation=false'); numericExit = $build.ExitCode; timedOut = $build.TimedOut; wallMilliseconds = $build.WallMs; shutdownExit = $build.BuildServerShutdownExit; rootExitDescendants = $build.RootExitDescendants; residualStatus = $build.ResidualStatus; cleanup = $build.Cleanup; phnStatus = $build.PhnStatus; rawStdoutSha256 = $build.RawStdoutSha256; rawStderrSha256 = $build.RawStderrSha256 }
    selfTest = @{ command = @('%USERPROFILE%\.dotnet\dotnet.exe', 'exec', '%TEMP%\<copy447-build>\artifacts\bin\Probe\release\Probe.dll', '--self-test'); numericExit = $self.ExitCode; timedOut = $self.TimedOut; wallMilliseconds = $self.WallMs; shutdownExit = $self.BuildServerShutdownExit; rootExitDescendants = $self.RootExitDescendants; residualStatus = $self.ResidualStatus; cleanup = $self.Cleanup; phnStatus = $self.PhnStatus; rawStdoutSha256 = $self.RawStdoutSha256; rawStderrSha256 = $self.RawStderrSha256 }
    pii = @{ command = @('%CFD_WRI_PYTHON%', 'tools/check-proof-pii.py'); numericExit = $pii.ExitCode; timedOut = $pii.TimedOut; wallMilliseconds = $pii.WallMs; phnStatus = $pii.PhnStatus; rawStdoutSha256 = $pii.RawStdoutSha256; rawStderrSha256 = $pii.RawStderrSha256 }
    cases = @($caseRecords)
    totalWallMilliseconds = $clock.ElapsedMilliseconds
    crashMeasurement = 'NOT ASSESSED'
}
$receipt | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $captureDir 'receipt.json') -Encoding utf8NoBOM
Assert-Copy447TempChild $artifactRoot 'copy447-build-'
Remove-Item -LiteralPath $artifactRoot -Recurse -Force
if (Test-Path -LiteralPath $artifactRoot) { throw 'COPY447-CLEANUP: temporary build artifacts remain' }
"COPY447 CAPTURE head=$gitHead cases=$($caseRecords.Count) crash_measurement=NOT_ASSESSED PHN=PASS total_ms=$($clock.ElapsedMilliseconds)"
} finally {
    Assert-Copy447TempChild $artifactRoot 'copy447-build-'
    if (Test-Path -LiteralPath $artifactRoot) {
        Remove-Item -LiteralPath $artifactRoot -Recurse -Force
        if (Test-Path -LiteralPath $artifactRoot) { throw 'COPY447-CLEANUP: temporary build artifacts remain after bounded attempt' }
    }
}
