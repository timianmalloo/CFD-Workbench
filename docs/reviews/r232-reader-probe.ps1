$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$packet = Join-Path $root 'docs/proof/win-gpu-f1-preflight/packet'
. (Join-Path $packet 'Control.ps1')
function Require([bool]$value, [string]$message) { if (-not $value) { throw $message } }
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $packet 'Invoke-Preflight.ps1'), [ref]$tokens, [ref]$errors)
Require ($errors.Count -eq 0) 'parse'
$definitions = @{}
foreach ($name in @('Open-LiveEvidenceStream','Read-LiveText','Read-GuardState','Assert-Guard')) {
    $node = $ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $name}, $true)
    Require ($null -ne $node) $name
    $definitions[$name] = $node.Extent.Text
    Invoke-Expression $node.Extent.Text
}
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryBase ('r232-independent-' + [guid]::NewGuid().ToString('N'))
$null = [IO.Directory]::CreateDirectory($fixture)
try {
    $Live = $fixture; $RunId = 'synthetic-reader'; $script:BeforeSha = 'synthetic-sha'
    $Guard = [pscustomobject]@{ HasExited=$false }
    $statePath = Join-Path $fixture 'completed-guard.state.json'
    $eventsPath = Join-Path $fixture 'completed-guard-events.jsonl'
    $state = @{state='running';session_id=$RunId;expected_sha256=$script:BeforeSha;completed_samples=7;last_sample_utc=[DateTimeOffset]::UtcNow.ToString('o')} | ConvertTo-Json -Compress
    $event = @{verdict='continue';session_id=$RunId;expected_sha256=$script:BeforeSha} | ConvertTo-Json -Compress
    $unicode = 'reader-' + [char]0x03bb + '-' + [char]0x6c34
    foreach ($encoding in @([Text.UTF8Encoding]::new($false),[Text.UTF8Encoding]::new($true),[Text.Encoding]::Unicode,[Text.Encoding]::BigEndianUnicode)) {
        [IO.File]::WriteAllText($statePath,$unicode,$encoding)
        Require ((Read-LiveText $statePath) -ceq $unicode) 'encoding/BOM changed'
    }
    [IO.File]::WriteAllText($statePath,$state,[Text.UTF8Encoding]::new($true))
    [IO.File]::WriteAllText($eventsPath,$event + "`r`n" + '{"partial":',[Text.UTF8Encoding]::new($true))
    $writer = [IO.FileStream]::new($eventsPath,[IO.FileMode]::Open,[IO.FileAccess]::Write,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    try {
        $oldRefused = $false
        try { $null = [IO.File]::ReadAllText($eventsPath) } catch { $oldRefused = $true }
        Require $oldRefused 'old reader sharing defect did not reproduce'
        Require ((Assert-Guard) -eq 7) 'writer-first read/complete-event failed'
    } finally { $writer.Dispose() }
    foreach ($bad in @('', '{"partial":', ($event + "`n" + '{"bad":' + "`n"), (($event -replace 'continue','stop') + "`n"))) {
        [IO.File]::WriteAllText($eventsPath,$bad,[Text.UTF8Encoding]::new($false))
        $refused = $false
        try { $null = Assert-Guard } catch { $refused = $true }
        Require $refused 'bad/incomplete/stop event accepted'
        $exclusive = [IO.FileStream]::new($eventsPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
        $exclusive.Dispose()
    }
    $held = Open-LiveEvidenceStream $eventsPath
    try {
        $renamed = Join-Path $fixture 'renamed.jsonl'
        [IO.File]::Move($eventsPath,$renamed)
        [IO.File]::WriteAllText($eventsPath,$event + "`n",[Text.UTF8Encoding]::new($false))
        Require ((Assert-Guard) -eq 7) 'replacement event read failed'
        [IO.File]::Delete($renamed)
    } finally { $held.Dispose() }
    $script:constructorStream = [IO.FileStream]::new($eventsPath,[IO.FileMode]::Open,[IO.FileAccess]::Write,[IO.FileShare]::ReadWrite)
    function Open-LiveEvidenceStream([string]$Path) { return $script:constructorStream }
    $refused = $false
    try { $null = Read-LiveText 'synthetic' } catch { $refused = $true }
    Require ($refused -and -not $script:constructorStream.CanWrite) 'constructor failure leaked stream'
    Invoke-Expression $definitions['Open-LiveEvidenceStream']
    Add-Type -TypeDefinition @'
using System;
using System.IO;
public sealed class R232ThrowingStream : Stream {
    public bool Disposed; public int Reads;
    public override bool CanRead { get { return true; } }
    public override bool CanWrite { get { return false; } }
    public override bool CanSeek { get { return false; } }
    public override long Length { get { throw new NotSupportedException(); } }
    public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
    public override void Flush() { }
    public override int Read(byte[] b,int o,int n) { Reads++; throw new IOException("synthetic read failure"); }
    public override long Seek(long o,SeekOrigin s) { throw new NotSupportedException(); }
    public override void SetLength(long n) { throw new NotSupportedException(); }
    public override void Write(byte[] b,int o,int n) { throw new NotSupportedException(); }
    protected override void Dispose(bool disposing) { Disposed=true; base.Dispose(disposing); }
}
'@
    $script:throwingStream = [R232ThrowingStream]::new()
    function Open-LiveEvidenceStream([string]$Path) { return $script:throwingStream }
    $refused = $false
    try { $null = Read-LiveText 'synthetic' } catch { $refused = $true }
    Require ($refused -and $script:throwingStream.Reads -gt 0 -and $script:throwingStream.Disposed) 'read exception leaked stream'
    Invoke-Expression $definitions['Open-LiveEvidenceStream']
    Write-Output 'R232-INDEPENDENT-PASS old-reader-red=1 encodings=4 writer-first=1 rename-delete=1 bad-events=4 constructor-disposal=1 read-disposal=1 live_calls=0'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    Require ($resolvedFixture.StartsWith($temporaryBase,[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($resolvedFixture).StartsWith('r232-independent-')) 'fixture cleanup outside declared temporary root'
    [IO.Directory]::Delete($resolvedFixture,$true)
}
