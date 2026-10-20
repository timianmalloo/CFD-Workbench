# F1 re-scope node C4 (Rulings 237, 240 (1) and (2)): the protected distro is a contract field with a `none` state (the
# registration check requires no protected row, the completed-L3 guard and the keepalive do not bind, the receipts say
# so), and the rootfs signature is verified on the Windows side by the Git-for-Windows gpgv against the reviewed
# cloud-image keyring, before any WSL effect. Offline-fast; both runtimes; no WSL. gpgv.exe and gpg.exe are the ones
# Git for Windows ships; the signature fixture under tests/fixtures/f1-rootfs-signature was signed by a throwaway key.
$ErrorActionPreference = 'Stop'; Set-StrictMode -Version Latest
$packet = Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\proof\win-gpu-f1-preflight\packet'
$fixtures = Join-Path $PSScriptRoot 'fixtures\f1-rootfs-signature'
. (Join-Path $packet 'Control.ps1')
function Require($condition, $message) { if (-not $condition) { throw $message } }
function Refuses([scriptblock]$action, [string]$message) {
    $refused = $false
    try { & $action | Out-Null } catch { $refused = $true }
    Require $refused $message
}
$errors = $null; $tokens = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'launcher parse failed' }
foreach ($name in @('Write-Lf', 'Write-Json', 'Open-LiveEvidenceStream', 'Read-LiveText', 'New-ProtectedRecord',
                    'Assert-Guard', 'Get-GpgvPath', 'Get-GpgvObservation', 'Convert-MsysPath', 'Invoke-RootfsSignature')) {
    $node = $ast.Find({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name }, $true)
    if ($null -eq $node) { throw "launcher function missing: $name" }
    Invoke-Expression $node.Extent.Text
}
$FIXTURE_SIGNER = 'F0ABAE6667730DD5ED887B2A37FAD5AAA451C6DE'
$FIXTURE_ROOTFS_SHA256 = '89fa28756c61ffb898294d920fc9fce84168e0eceeff5389fabf074bfd8d8989'   # sha256 of "fixture-rootfs\n"
$REAL_SIGNERS = @('D2EB44626FDDC30B513D5BB71A5D6C4C7DB87C81', '4A3CE3CD565D7EB5C810E2B97FF3F408476CF100')

# --- Registrations_NoneDeclared_StockDistroOnly_Admitted ---------------------------------------------------------------
$path = 'C:\Projects\CFD-Workbench-wsl\cfdw-petsc-cuda132-f1'
$stock = [pscustomobject]@{ DistributionName='Ubuntu-24.04'; PSChildName='{aaaaaaaa-1111-2222-3333-444444444444}'; Version=2; BasePath='C:\fixture\wsl\stock' }
Assert-FreshRegistrations @($stock) @('Ubuntu-24.04') $false 1 $path 'none'
Assert-FreshRegistrations @($stock) @('Ubuntu-24.04') $false 2 $path 'none'
Assert-FreshRegistrations @() @() $false 1 $path 'none'
$namedLikeProtected = [pscustomobject]@{ DistributionName='cfdw-openfoam2512'; PSChildName='{bbbbbbbb-1111-2222-3333-444444444444}'; Version=2; BasePath='C:\fixture\wsl\elsewhere' }
# DR-4 (node R, Security & Identity Architect F2): 'none' says the host carries no protected distro, so a row with the
# protected name, GUID or path refuses; any other registration is neither required nor refused.
Refuses { Assert-FreshRegistrations @($stock, $namedLikeProtected) @('Ubuntu-24.04', 'cfdw-openfoam2512') $false 1 $path 'none' } 'a row named like the protected distro was admitted under none'
$guidLikeProtected = [pscustomobject]@{ DistributionName='other'; PSChildName='{b7566a82-9cee-47ce-a246-f83632353cea}'; Version=2; BasePath='C:\fixture\wsl\other' }
Refuses { Assert-FreshRegistrations @($stock, $guidLikeProtected) @('Ubuntu-24.04', 'other') $false 1 $path 'none' } 'a row with the protected GUID was admitted under none'
$pathLikeProtected = [pscustomobject]@{ DistributionName='other'; PSChildName='{cccccccc-1111-2222-3333-444444444444}'; Version=2; BasePath=[Environment]::ExpandEnvironmentVariables('%USERPROFILE%\AppData\Local\CFDWorkbench\qualification\wsl\cfdw-openfoam2512') } # machine-path-ok: fixture only, value not emitted.
Refuses { Assert-FreshRegistrations @($stock, $pathLikeProtected) @('Ubuntu-24.04', 'other') $false 1 $path 'none' } 'a row at the protected path was admitted under none'
$other = [pscustomobject]@{ DistributionName='other'; PSChildName='{dddddddd-1111-2222-3333-444444444444}'; Version=2; BasePath='C:\fixture\wsl\other' }
Assert-FreshRegistrations @($stock, $other) @('Ubuntu-24.04', 'other') $false 1 $path 'none'   # any other row: neither required nor refused
$targetRow = [pscustomobject]@{ DistributionName='cfdw-petsc-cuda132-f1'; PSChildName='{11111111-2222-3333-4444-555555555555}'; Version=2; BasePath=$path }
Refuses { Assert-FreshRegistrations @($stock, $targetRow) @('Ubuntu-24.04') $false 1 $path 'none' } 'a target row was admitted under none'
Refuses { Assert-FreshRegistrations @($stock) @('Ubuntu-24.04') $true 1 $path 'none' } 'a present target path was admitted under none'
Refuses { Assert-FreshRegistrations @($stock) @('Ubuntu-24.04') $false 1 $path 'other' } 'an unknown protected state was admitted'
Refuses { Assert-FreshRegistrations @($stock) @('Ubuntu-24.04') $false 1 $path '' } 'an empty protected state was admitted'

# --- Registrations_ProtectedDeclaredButAbsent_Refuses --------------------------------------------------------------------
Refuses { Assert-FreshRegistrations @($stock) @('Ubuntu-24.04') $false 1 $path } 'the declared protected distro is absent but was admitted (default)'
Refuses { Assert-FreshRegistrations @($stock) @('Ubuntu-24.04') $false 1 $path 'cfdw-openfoam2512' } 'the declared protected distro is absent but was admitted'
Refuses { Assert-FreshRegistrations @($stock, $namedLikeProtected) @('Ubuntu-24.04', 'cfdw-openfoam2512') $false 1 $path 'cfdw-openfoam2512' } 'a protected row with another GUID/path was admitted when declared'

# The attempt-2 exclusivity check under none: no keepalive identity is required; a foreign WSL actor still refuses.
$now = [DateTimeOffset]::UtcNow
$rows = @([pscustomobject]@{ ProcessId=1; ParentProcessId=0; Name='explorer.exe'; CommandLine='explorer'; CreationDate=$now })
Assert-AttemptExclusivity $rows @() 'none'
$actor = [pscustomobject]@{ ProcessId=2; ParentProcessId=1; Name='wsl.exe'; CommandLine='"C:\WINDOWS\System32\wsl.exe" -d Ubuntu-24.04'; CreationDate=$now }
Refuses { Assert-AttemptExclusivity @($rows + $actor) @() 'none' } 'a foreign WSL actor was admitted under none'
Refuses { Assert-AttemptExclusivity $rows @() '{b7566a82-9cee-47ce-a246-f83632353cea}' } 'the declared keepalive is absent but was admitted'
Refuses { Assert-AttemptExclusivity $rows @() 'other' } 'an unknown protected key was admitted'

# --- GuardState_None_RecordedInReceipt ------------------------------------------------------------------------------
$Protected = 'none'; $Guard = $null; $RunId = 'fixture-run'
$record = New-ProtectedRecord
Require ($record.protected -ceq 'none' -and $record.guard -ceq 'not applicable') 'none is not recorded as protected: none, guard: not applicable'
Require ((Assert-Guard) -ceq 'not applicable') 'the guard was asserted under none'
$Protected = 'cfdw-openfoam2512'
$record = New-ProtectedRecord
Require ($record.protected -ceq 'cfdw-openfoam2512' -and $record.guard -ceq 'binds') 'a declared protected distro does not record a binding guard'
Refuses { Assert-Guard } 'a declared guard that was never started was asserted'
$Protected = $null
$record = New-ProtectedRecord
Require ($record.protected -ceq 'Not recorded' -and $record.guard -ceq 'Not recorded') 'an unbound protected state is not Not recorded'
# Structural half on the AST (never raw text: a comment cannot satisfy it; node R, Test Architect F2): both receipts carry
# guard=$protectedRecord.guard; at least four `$Protected -cne 'none'` conditions guard the binding; $Protected starts
# null and is bound from the contract; no string in the launcher names verify-rootfs.sh.
$guardPairs = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.HashtableAst] }, $true) |
    ForEach-Object { $_.KeyValuePairs } |
    Where-Object { $_.Item1.Extent.Text -ceq 'guard' -and $_.Item2.Extent.Text -ceq '$protectedRecord.guard' })
Require ($guardPairs.Count -ge 2) 'a receipt does not record the guard state'
$conditions = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.BinaryExpressionAst] -and
    $n.Operator -eq [Management.Automation.Language.TokenKind]::Cne -and $n.Left.Extent.Text -ceq '$Protected' -and $n.Right.Extent.Text -ceq "'none'" }, $true))
Require ($conditions.Count -ge 4) 'the protected binding is not conditional on the contract field'
$assignments = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -ceq '$Protected' }, $true) | ForEach-Object { $_.Right.Extent.Text })
Require (($assignments -ccontains '$null') -and ($assignments -ccontains '[string]$contractData.protected')) 'the protected distro is still a launcher constant'
$rootfsStrings = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.StringConstantExpressionAst] -and $n.Value -like '*verify-rootfs*' }, $true))
Require ($rootfsStrings.Count -eq 0) 'the rootfs signature still runs inside a distro'

# --- Rootfs_GpgvAbsent_RefusesWithPath ------------------------------------------------------------------------------
$tmp = Join-Path ([IO.Path]::GetTempPath()) ('cfdw-f1-protected-none-' + [Guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Path $tmp | Out-Null
try {
    $execPath = Join-Path $tmp 'Git\clangarm64\libexec\git-core'
    New-Item -ItemType Directory -Path $execPath -Force | Out-Null
    $expectedGpgv = Join-Path $tmp 'Git\usr\bin\gpgv.exe'
    $refusal = ''
    try { $null = Get-GpgvPath $execPath } catch { $refusal = $_.Exception.Message }
    Require ($refusal.Contains($expectedGpgv)) "an absent gpgv.exe did not refuse with the searched path named: $refusal"
    New-Item -ItemType Directory -Path (Split-Path -Parent $expectedGpgv) -Force | Out-Null
    [IO.File]::WriteAllBytes($expectedGpgv, [byte[]]@(77, 90))
    Require ((Get-GpgvPath $execPath) -ceq $expectedGpgv) 'gpgv.exe under the Git root was not found by walking up'
    Require ((Get-GpgvPath (Join-Path $tmp 'Git')) -ceq $expectedGpgv) 'the Git root itself was not searched'
    $observed = Get-GpgvObservation
    Require ($observed.path -like '*\usr\bin\gpgv.exe' -and (Test-Path -LiteralPath $observed.path -PathType Leaf)) "this host's gpgv was not observed: $($observed.path)"
    Require ($observed.version -like 'gpgv (GnuPG)*') "gpgv version not observed: $($observed.version)"

    # The MSYS gpgv reads a drive-letter keyring path as a URL scheme (measured 2026-10-19); the launcher hands it the MSYS form.
    Require ((Convert-MsysPath 'D:\Projects\x\keyring.gpg') -ceq '/d/Projects/x/keyring.gpg') 'MSYS path conversion changed'
    Refuses { Convert-MsysPath 'D:\Program Files\keyring.gpg' } 'a path with whitespace was converted'
    Refuses { Convert-MsysPath 'relative\keyring.gpg' } 'a relative path was converted'

    # --- Rootfs_UnapprovedSigner_RefusedOnWindows ------------------------------------------------------------------
    $Live = $tmp; $PacketRoot = $packet
    function Invoke-Child([string]$File, [string[]]$Arguments, [string]$Label, [int]$CapSeconds, [bool]$GuardRequired = $true, [bool]$NewWork = $false) {
        # The child boundary of the launcher, made synchronous for the suite; a non-zero exit refuses as in the launcher.
        $stdout = Join-Path $Live "$Label.stdout.txt"; $stderr = Join-Path $Live "$Label.stderr.txt"
        $process = Start-Process -FilePath $File -ArgumentList $Arguments -PassThru -WindowStyle Hidden `
            -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        $null = $process.Handle
        if (-not $process.WaitForExit($CapSeconds * 1000)) { Stop-Process -Id $process.Id -Force; throw "$Label exceeded its cap" }
        if ($process.ExitCode -ne 0) { throw "$Label exited $($process.ExitCode): $([IO.File]::ReadAllText($stderr))" }
        return $stdout
    }
    $gpgv = $observed.path
    $keyring = Join-Path $fixtures 'fixture-keyring.gpg'
    $sums = Join-Path $fixtures 'SHA256SUMS'
    $signature = Join-Path $fixtures 'SHA256SUMS.gpg'
    $rootfs = Join-Path $tmp 'ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz'
    [IO.File]::WriteAllBytes($rootfs, [Text.Encoding]::ASCII.GetBytes("fixture-rootfs`n"))
    $body = Get-Content (Join-Path (Split-Path -Parent $packet) 'run-1\invocation-contract.json') -Raw | ConvertFrom-Json
    foreach ($field in @{ schema='cfdw-f1-r217-invocation/2'; architecture='arm64'; protected='none'
                          host_start_floor_bytes=19684091961; host_reserve_bytes=10737418240 }.GetEnumerator()) {
        $body | Add-Member NoteProperty $field.Key $field.Value -Force
    }
    $body.inputs.psobject.Properties.Remove('b2_lock')
    $body.inputs | Add-Member NoteProperty ubuntu_cloudimage_keyring ([pscustomobject]@{ path='C:\reviewed\ubuntu-cloudimage-keyring.gpg'; sha256=('c' * 64) }) -Force
    $body.inputs.rootfs.path = 'C:\reviewed\ubuntu-noble-wsl-arm64-wsl.rootfs.tar.gz'
    $body.inputs.rootfs.sha256 = 'fecec1d9b7b750c12c109edb49c13c1006f4a2efabb9b8bf341f11c4c9f2ef11'
    $contractPath = Join-Path $tmp 'invocation-contract.json'
    function Write-Contract([string[]]$Signers) {
        $body.allowed_signers.ubuntu_rootfs = $Signers
        Write-Json $contractPath $body
        return (Get-FileHash -Algorithm SHA256 -LiteralPath $contractPath).Hash.ToLowerInvariant()
    }
    $status = Join-Path $tmp 'rootfs-signature.gpgv.txt'
    $sha = Write-Contract @($FIXTURE_SIGNER)
    $signed = Invoke-RootfsSignature $gpgv $keyring $signature $sums $rootfs $status $contractPath $sha $FIXTURE_ROOTFS_SHA256
    Require ($signed -ceq $FIXTURE_ROOTFS_SHA256) 'the approved fixture signer did not pass'
    Require (([IO.File]::ReadAllText($status)).Contains("VALIDSIG $FIXTURE_SIGNER")) 'the gpgv status was not recorded'
    Require (([IO.File]::ReadAllText((Join-Path $tmp 'rootfs-signature.txt'))).StartsWith("ROOTFS-SIGNED-PASS sha256=$FIXTURE_ROOTFS_SHA256")) 'the pass line was not recorded'
    $sha = Write-Contract $REAL_SIGNERS   # the fixture key is not an Ubuntu cloud-image signer
    Refuses { Invoke-RootfsSignature $gpgv $keyring $signature $sums $rootfs $status $contractPath $sha $FIXTURE_ROOTFS_SHA256 } 'an unapproved signer was admitted on Windows'
    $sha = Write-Contract @($FIXTURE_SIGNER)
    $realKeyring = Join-Path (Split-Path -Parent $packet) 'inputs\ubuntu-cloudimage-keyring.gpg'
    Refuses { Invoke-RootfsSignature $gpgv $realKeyring $signature $sums $rootfs $status $contractPath $sha $FIXTURE_ROOTFS_SHA256 } 'a signature without a key in the keyring was admitted'
    Refuses { Invoke-RootfsSignature $gpgv $keyring $signature $sums $rootfs $status $contractPath $sha ('f' * 64) } 'a signed sums line that differs from the contract pin was admitted'
    [IO.File]::WriteAllBytes($rootfs, [Text.Encoding]::ASCII.GetBytes("tampered`n"))
    Refuses { Invoke-RootfsSignature $gpgv $keyring $signature $sums $rootfs $status $contractPath $sha $FIXTURE_ROOTFS_SHA256 } 'a rootfs whose bytes differ from the signed line was admitted'
    [IO.File]::WriteAllBytes($rootfs, [Text.Encoding]::ASCII.GetBytes("fixture-rootfs`n"))
    Refuses { Invoke-RootfsSignature (Join-Path $tmp 'missing\gpgv.exe') $keyring $signature $sums $rootfs $status $contractPath $sha $FIXTURE_ROOTFS_SHA256 } 'an absent gpgv was admitted at the signature step'
} finally {
    Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Output ("protected none controls: PASS ring=offline-fast runtime={0} wsl_launches=0" -f $PSVersionTable.PSEdition)
