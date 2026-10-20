# F1 dry rehearsal (Ruling 245 (3)): the launcher's Windows-side paths that had never run live, exercised with no WSL
# effect, against real processes and the real repository attributes. Runs red before every attempt that would fail on
# them, and green before an attempt is spent.
#
#   pwsh -NoProfile -File tools/run-f1-dry-rehearsal.ps1 -RunRoot docs/proof/win-gpu-f1-preflight/attempt-4 [-Receipt <path>] [-Repetitions 5]
#
# Steps (each prints `== rehearsal <name> ok|RED ...`):
#   own-child     Register-AttemptLauncher (loaded from the launcher's AST) against <Repetitions> real child processes
#                 (cmd.exe pinging localhost): the identity equality across the CIM and .NET timestamps must hold every
#                 time (TIMESTAMP-IDENTITY-ACROSS-PRECISIONS: one literal on both sides proves nothing).
#   attributes    `git check-attr text` on <RunRoot>/{checkpoint,final,live}/probe.txt must be `unset`, or the
#                 launcher's checkpoint bind refuses after Git normalizes a CRLF capture (RAW-CAPTURE-BLOB-MISMATCH 4).
#   bind          a freeze-commit-bind cycle (manifest.py freeze, git add/commit, manifest.py verify) of a CRLF capture
#                 under <RunRoot>/checkpoint in a detached scratch worktree of this repository, then removed.
# Exit 0 green, 1 red, 2 usage. Ring: the F1 verification runner (tools/run-f1-verification.py) and the attempt
# README's pre-attempt list; not the fast ring (it spawns processes, ~10-20 s). Receipt schema cfdw-f1-dry-rehearsal/1,
# scrubbed of the repository path and the user profile.
param(
    [Parameter(Mandatory = $true)][string]$RunRoot,
    [string]$Receipt = '',
    [int]$Repetitions = 5
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$PacketRoot = Join-Path $RepoRoot 'docs\proof\win-gpu-f1-preflight\packet'
if ($RunRoot -notmatch '^docs/proof/win-gpu-f1-preflight/attempt-[0-9]+$') { Write-Output "usage: -RunRoot docs/proof/win-gpu-f1-preflight/attempt-<n>"; exit 2 }
. (Join-Path $PacketRoot 'Control.ps1')
$clock = [Diagnostics.Stopwatch]::StartNew()
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('cfdw-f1-rehearsal-' + [guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Path $scratch | Out-Null
$steps = [Collections.Generic.List[object]]::new()
function Scrub([string]$Text) {
    $value = $Text.Replace($RepoRoot, '<repo>').Replace($scratch, '<scratch>')
    $profile = [Environment]::GetFolderPath('UserProfile')
    if ($profile) { $value = $value.Replace($profile, '<user>') }
    return $value
}
function Record([string]$Name, [bool]$Ok, [string]$Summary, [hashtable]$Detail = @{}) {
    $row = [ordered]@{ name = $Name; ok = $Ok; summary = (Scrub $Summary); seconds = [Math]::Round($clock.Elapsed.TotalSeconds, 3) }
    foreach ($key in $Detail.Keys) { $row[$key] = $Detail[$key] }
    $steps.Add($row)
    Write-Output ('== rehearsal {0,-11} {1} {2}' -f $Name, $(if ($Ok) { 'ok ' } else { 'RED' }), (Scrub $Summary))
}

# --- own-child: the launcher's own registration function against real processes
$errors = $null; $tokens = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PacketRoot 'Invoke-Preflight.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'launcher does not parse' }
$node = $ast.Find({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Register-AttemptLauncher' }, $true)
Invoke-Expression $node.Extent.Text
$Live = Join-Path $scratch 'live'
New-Item -ItemType Directory -Path $Live | Out-Null
$script:OwnedLaunchers = @()
function Write-Json([string]$Path, [object]$Value) { [IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 20) + "`n"), [Text.UTF8Encoding]::new($false)) }
$refused = 0
$samples = @()
for ($i = 1; $i -le $Repetitions; $i++) {
    $child = Start-Process -FilePath 'cmd.exe' -ArgumentList @('/c', 'ping -n 3 127.0.0.1 >nul') -WindowStyle Hidden -PassThru
    $null = $child.Handle
    try {
        try { Register-AttemptLauncher $child; $samples += 'ok' }
        catch { $refused++; $samples += $_.Exception.Message }
    } finally {
        if (-not $child.HasExited) { Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue }
    }
}
Record 'own-child' ($refused -eq 0) ("registrations {0}/{1} passed" -f ($Repetitions - $refused), $Repetitions) @{ refused = $refused; samples = @($samples) }

# --- attributes: the run root's capture folders keep their bytes
$probes = @('checkpoint', 'final', 'live') | ForEach-Object { "$RunRoot/$_/probe.txt" }
$attr = @(& git -C $RepoRoot check-attr text -- @probes 2>&1)
$unset = @($attr | Where-Object { $_ -match ': text: unset$' }).Count
Record 'attributes' ($unset -eq $probes.Count) ("{0}/{1} capture folders have text unset ({2})" -f $unset, $probes.Count, ($attr -join '; ')) @{ run_root = $RunRoot; lines = @($attr) }

# --- bind: freeze, commit and verify a CRLF capture in a detached scratch worktree
$wt = Join-Path $scratch 'wt'
$bindOk = $false
$bindSummary = 'not run'
try {
    # No checkout: the repository carries paths too long for a scratch under %TEMP% (git refuses with "Filename too
    # long"); the bind needs only .gitattributes and Git's metadata, so that one file is checked out.
    $added = @(& git -C $RepoRoot worktree add --detach --no-checkout -q $wt HEAD 2>&1)
    if ($LASTEXITCODE -ne 0) { throw ('scratch worktree add failed: ' + ($added -join '; ')) }
    $null = & git -C $wt checkout -q HEAD -- .gitattributes 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'scratch .gitattributes checkout failed' }
    $liveDir = Join-Path $wt ($RunRoot.Replace('/', '\') + '\live')
    $frozenDir = Join-Path $wt ($RunRoot.Replace('/', '\') + '\checkpoint')
    New-Item -ItemType Directory -Path $liveDir -Force | Out-Null
    [IO.File]::WriteAllBytes((Join-Path $liveDir 'host-identity.json'), [Text.Encoding]::ASCII.GetBytes("{`"schema`": `"cfdw-f1-host-identity/1`", `"architecture`": `"rehearsal`"}`n"))   # manifest.REQUIRED
    [IO.File]::WriteAllBytes((Join-Path $liveDir 'host-identity.stdout.txt'), [Text.Encoding]::ASCII.GetBytes("HOST-IDENTITY-PASS architecture=rehearsal cpu=1 gpu=1`r`n"))
    [IO.File]::WriteAllBytes((Join-Path $liveDir 'probe.stdout.txt'), [Text.Encoding]::ASCII.GetBytes("line one`r`nline two`r`n"))
    $freeze = & py -3 (Join-Path $PacketRoot 'manifest.py') freeze --live $liveDir --frozen $frozenDir 2>&1
    if ($LASTEXITCODE -ne 0) { throw "freeze failed: $freeze" }
    $relative = $RunRoot + '/checkpoint'
    $null = & git -C $wt add -f -- $relative 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'scratch add failed' }
    $null = & git -C $wt -c user.name=rehearsal -c user.email=rehearsal@example.invalid commit -q -m 'rehearsal: freeze-commit-bind' 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'scratch commit failed' }
    $sha = (& git -C $wt rev-parse HEAD).Trim()
    $verify = @(& py -3 (Join-Path $PacketRoot 'manifest.py') verify --repo $wt --frozen $frozenDir --commit $sha 2>&1)
    $bindOk = ($LASTEXITCODE -eq 0)
    $bindSummary = ($verify -join '; ')
} catch {
    $bindSummary = $_.Exception.Message
} finally {
    if (Test-Path -LiteralPath $wt) { $null = & git -C $RepoRoot worktree remove --force $wt 2>&1 }
    $null = & git -C $RepoRoot worktree prune 2>&1
}
Record 'bind' $bindOk $bindSummary @{ run_root = $RunRoot }

# --- result
$green = @($steps | Where-Object { -not $_.ok }).Count -eq 0
$head = (& git -C $RepoRoot rev-parse HEAD).Trim()
$line = ('F1-DRY-REHEARSAL-{0} run_root={1} head={2} own_child={3}/{4} attributes={5} bind={6} total={7:F1} s' -f $(if ($green) { 'GREEN' } else { 'RED' }), $RunRoot, $head.Substring(0, 9), ($Repetitions - $refused), $Repetitions, $(if ($unset -eq $probes.Count) { 'ok' } else { 'red' }), $(if ($bindOk) { 'ok' } else { 'red' }), $clock.Elapsed.TotalSeconds)
if ($Receipt) {
    # Named $document, never $receipt: PowerShell variable names are case-insensitive, and a $receipt would replace the
    # -Receipt parameter (the first run wrote its receipt to a file named after the dictionary's type).
    $document = [ordered]@{
        schema = 'cfdw-f1-dry-rehearsal/1'; utc = [DateTimeOffset]::UtcNow.ToString('o'); head = $head; run_root = $RunRoot
        powershell_version = $PSVersionTable.PSVersion.ToString()
        process_architecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
        repetitions = $Repetitions; result = $(if ($green) { 'green' } else { 'red' }); steps = @($steps); line = $line
    }
    $parent = Split-Path -Parent $Receipt
    if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    [IO.File]::WriteAllText($Receipt, (($document | ConvertTo-Json -Depth 8) -replace "`r`n", "`n") + "`n", [Text.UTF8Encoding]::new($false))
}
Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
Write-Output $line
exit $(if ($green) { 0 } else { 1 })
