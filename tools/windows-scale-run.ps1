# Ring: Windows preparation controls; Run is the separately authorized two-scale contract.
# Per build/check/readback child: 120000 ms including cleanup (root receives at most half).
param([ValidateSet('Run','Select')][string]$Action, [ValidateSet(150,200)][int]$Scale=150,
      [string]$PythonPath, [string]$OutputDirectory)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'windows-runner.ps1')

function Invoke-WriScaleSequence {
    param([scriptblock]$Preflight,[scriptblock]$Build,[scriptblock]$Select,
          [scriptblock]$Contract,[scriptblock]$Restore,[scriptblock]$Readback)
    & $Preflight '150% (Recommended)'
    try {
        & $Build
        foreach ($scale in @(150,200)) { & $Select $scale; & $Contract $scale }
    } finally {
        try { & $Restore } finally { & $Readback }
    }
}
function Get-WriScaleGroups {
    return @(
        @{Mode='shell-window';Names=@('KeyBindings_MenuGesture_NotBound')},
        @{Mode='views';Names=@('ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView','ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack','Elevation_SideSelectedStation_RenderedFullWeight','View3d_SelectedStation_RenderedWidthAndChip','View3d_ChipBorderSampler_FindsStationColourInTheLoggedWindowsBlock','Elevation_ChipBorderSampler_HoldsHalfOfASplitLine','View3d_CubeFocusRing_GapOnCurrentFaceThreeToOne')},
        @{Mode='properties-cells';Names=@('PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs','PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused','PropertiesPane_Density_EveryTargetAtLeast24','PropertiesPane_Density_EveryInputDeclaresMinHeightOf24')},
        @{Mode='readiness';Names=@('ModelArea_Views_SeparatedByGutterAndFramed','Analysis_FourViews_At1280x800_AndGeometryUnchangedAt1500x870')}
    )
}
function Select-WriScale([ValidateSet(150,200)][int]$Scale) {
    # Reuse the verified R179 ItemContainer/Realize/reacquire/SelectionItem shape.
    # No group expansion or scrolling: the read-only preflight requires those surfaces prepared.
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    $ae=[System.Windows.Automation.AutomationElement]
    $ts=[System.Windows.Automation.TreeScope]
    function Find-ScaleControl([string]$Id) {
        $frame=$null
        foreach ($window in $ae::RootElement.FindAll($ts::Children,[System.Windows.Automation.Condition]::TrueCondition)) {
            if ($window.Current.Name -eq 'Settings' -and $window.Current.ClassName -eq 'ApplicationFrameWindow') { $frame=$window; break }
        }
        if (-not $frame) { throw 'WRI-SCALE: exact Settings frame absent' }
        $control=$frame.FindFirst($ts::Descendants,[System.Windows.Automation.PropertyCondition]::new($ae::AutomationIdProperty,$Id))
        if (-not $control) { throw "WRI-SCALE: exact control absent: $Id" }
        return $control
    }
    $display=Find-ScaleControl 'Display1'
    $main=Find-ScaleControl 'SystemSettings_Display_MainMonitor_CheckBox'
    if (-not $display.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected -or
        $main.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On) { throw 'WRI-SCALE: selected Display1 must remain main monitor' }
    $wanted=if ($Scale -eq 150) { '150% (Recommended)' } else { '200%' }
    $combo=Find-ScaleControl 'SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox'
    if (-not $combo.Current.IsEnabled -or $combo.Current.IsOffscreen) { throw 'WRI-SCALE: scale combo must already be visible and enabled' }
    $item=$combo.GetCurrentPattern([System.Windows.Automation.ItemContainerPattern]::Pattern).FindItemByProperty($null,$ae::NameProperty,$wanted)
    if (-not $item) { throw 'WRI-SCALE: exact scale item absent' }
    $virtual=$null
    if ($item.TryGetCurrentPattern([System.Windows.Automation.VirtualizedItemPattern]::Pattern,[ref]$virtual)) { $virtual.Realize() }
    $combo=Find-ScaleControl 'SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox'
    $item=$combo.GetCurrentPattern([System.Windows.Automation.ItemContainerPattern]::Pattern).FindItemByProperty($null,$ae::NameProperty,$wanted)
    if (-not $item) { throw 'WRI-SCALE: exact item absent after reacquisition' }
    $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $selectionClock=[Diagnostics.Stopwatch]::StartNew()
    do {
        $combo=Find-ScaleControl 'SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox'
        $selection=$combo.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection()
        $actual=[string]::Join(',',@($selection | ForEach-Object { $_.Current.Name }))
        if ($actual -eq $wanted) { "SETTINGS_SELECTED=$actual"; return }
        [Threading.Thread]::Sleep(100)
    } while ($selectionClock.ElapsedMilliseconds -lt 10000)
    throw 'WRI-SCALE: selected scale did not settle within 10000 ms'
}
function Invoke-WriScaleRecordedChild {
    param($Context,[string]$Label,[string]$Exe,[string[]]$Arguments,[string]$Only,
          [long]$DeadlineMs,[int]$ChildCeilingMs=120000,
          [ValidateSet('Normal','BuildVerifier')][string]$Mode='Normal')
    $previousOnly=$env:CFD_TEST_ONLY
    $env:CFD_TEST_ONLY=$Only
    $row=[ordered]@{Label=$Label;Command=@($Exe)+$Arguments;Selector=$Only;
        StartedUtc=[DateTimeOffset]::UtcNow.ToString('o');DeadlineMs=$DeadlineMs;ChildCeilingMs=$ChildCeilingMs;
        EntryElapsedMs=$Context.Clock.ElapsedMilliseconds;ExitCode=$null;FailureType=$null}
    try {
        $result=Invoke-WriChild -Repo $Context.Repo -Exe $Exe -Arguments $Arguments -Clock $Context.Clock `
            -CeilingMs $DeadlineMs -ChildCeilingMs $ChildCeilingMs -PythonPath $Context.Toolchain.Python `
            -Mode $Mode -Toolchain $Context.Toolchain
        Assert-WriNumericExit $result.ExitCode
        $row.ExitCode=$result.ExitCode
        foreach ($property in @('TimedOut','WallMs','RetainedHandle','Stdout','Stderr','RootExitDescendants','Cleanup','ResidualStatus','BuildServerShutdownExit','RawStdoutSha256','RawStderrSha256','PhnStatus','SubstitutionBinding')) { $row[$property]=$result.$property }
        return $result
    } catch { $row.FailureType=$_.Exception.GetType().Name; throw }
    finally {
        $env:CFD_TEST_ONLY=$previousOnly
        $row.EndedUtc=[DateTimeOffset]::UtcNow.ToString('o')
        $row.FinalElapsedMs=$Context.Clock.ElapsedMilliseconds
        [void]$Context.Records.Add([pscustomobject]$row)
    }
}
function Assert-WriScaleContext([string]$Text,[string]$Mode,[double]$Expected) {
    $lines=@($Text -split '\r?\n' | Where-Object { $_ -match '^SCALE_CONTEXT ' })
    if ($lines.Count -ne 1 -or $lines[0] -notmatch ("^SCALE_CONTEXT mode="+[regex]::Escape($Mode)+" RenderScaling=(\S+) PrimaryScaling=(\S+) ")) { throw 'WRI-SCALE: one exact in-process scale line required' }
    $render=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture)
    $primary=[double]::Parse($Matches[2],[Globalization.CultureInfo]::InvariantCulture)
    if ($render -ne $Expected -or $primary -ne $Expected) { throw 'WRI-SCALE: in-process scale mismatch' }
    $firstCheck=[regex]::Match($Text,'(?m)^(PASS|FAIL) ')
    if ($firstCheck.Success -and $Text.IndexOf($lines[0],[StringComparison]::Ordinal) -gt $firstCheck.Index) { throw 'WRI-SCALE: scale line followed first check' }
}
function Invoke-WriScaleRun([string]$PythonPath,[string]$OutputDirectory) {
    $repo=Split-Path -Parent $PSScriptRoot
    $clock=[Diagnostics.Stopwatch]::StartNew()
    $runCeiling=1200000
    $executionDeadline=1020000 # Reserve 180 s for UIA restore, fresh readback, source proof and PHN publication.
    Assert-WriSourceClean $repo $clock $executionDeadline
    $baseline=Get-WriSourceFingerprint $repo $clock $executionDeadline
    $toolchain=Assert-WriToolchain -Repo $repo -PythonPath $PythonPath
    if (-not $OutputDirectory) { throw 'WRI-SCALE: explicit proof output directory required' }
    $output=[IO.Path]::GetFullPath($OutputDirectory)
    $proofRoot=[IO.Path]::GetFullPath((Join-Path $repo 'docs/proof'))+[IO.Path]::DirectorySeparatorChar
    if (-not $output.StartsWith($proofRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'WRI-SCALE: output must be inside repository docs/proof' }
    if (Test-Path -LiteralPath $output) { throw 'WRI-SCALE: output directory already exists; evidence is append-only' }
    [void][IO.Directory]::CreateDirectory($output)
    $context=[pscustomobject]@{Repo=$repo;Clock=$clock;Toolchain=$toolchain;Records=[Collections.Generic.List[object]]::new();RestoreExit=$null;ReadbackExit=$null;ContractFailed=$false}
    $engine=(Get-Process -Id $PID).Path
    $driver=Join-Path $PSScriptRoot 'windows-scale-run.ps1'
    $preflightPath=Join-Path $PSScriptRoot 'windows-settings-preflight.ps1'
    $dll=Join-Path $repo 'tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll'
    $failure=$null
    try {
        Invoke-WriScaleSequence -Preflight {
            param($expected)
            $checked=Invoke-WriScaleRecordedChild $context 'preflight-initial' $engine @('-NoProfile','-File',$preflightPath,'-Action','Preflight','-ExpectedScale',$expected) '' $executionDeadline 60000
            if ($checked.ExitCode -ne 0) { throw 'WRI-PREFLIGHT: initial read-only preflight rejected' }
        } -Build {
            $built=Invoke-WriScaleRecordedChild $context 'build' $toolchain.Dotnet @('build','CFDWorkbench.slnx','-c','Release','-nologo','-v','q') '' $executionDeadline 120000 BuildVerifier
            if ($built.ExitCode -ne 0) { throw 'WRI-BUILD: pinned build failed' }
        } -Select {
            param($scale)
            $selected=Invoke-WriScaleRecordedChild $context "select-$scale" $engine @('-NoProfile','-File',$driver,'-Action','Select','-Scale',[string]$scale) '' $executionDeadline 60000
            if ($selected.ExitCode -ne 0) { throw 'WRI-SCALE: UIA selection failed' }
            $expected=if ($scale -eq 150) { '150% (Recommended)' } else { '200%' }
            $checked=Invoke-WriScaleRecordedChild $context "preflight-$scale" $engine @('-NoProfile','-File',$preflightPath,'-Action','Preflight','-ExpectedScale',$expected) '' $executionDeadline 60000
            if ($checked.ExitCode -ne 0) { throw 'WRI-PREFLIGHT: selected-scale read-only verification rejected' }
        } -Contract {
            param($scale)
            foreach ($group in Get-WriScaleGroups) {
                $checked=Invoke-WriScaleRecordedChild $context "$scale-$($group.Mode)" $toolchain.Dotnet @('exec',$dll,"--$($group.Mode)") ($group.Names -join ',') $executionDeadline 120000 BuildVerifier
                Assert-WriScaleContext $checked.Stdout $group.Mode ($scale/100.0)
                $observed=@([regex]::Matches($checked.Stdout,'(?m)^(?:PASS|FAIL) (\S+)') | ForEach-Object { $_.Groups[1].Value })
                if ((($observed | Sort-Object) -join ',') -cne (($group.Names | Sort-Object) -join ',')) { throw 'WRI-CONTRACT: missing, duplicated or unselected check' }
                if ($group.Mode -eq 'views' -and @([regex]::Matches($checked.Stdout,'(?m)^P3 ')).Count -ne 1) { throw 'WRI-CONTRACT: P3 observation missing or duplicated' }
                if ($group.Mode -eq 'properties-cells' -and @([regex]::Matches($checked.Stdout,'(?m)^ITEM6 ')).Count -ne 3) { throw 'WRI-CONTRACT: three item-6 observations required' }
                if ($checked.ExitCode -ne 0) { $context.ContractFailed=$true }
            }
        } -Restore {
            $restored=Invoke-WriScaleRecordedChild $context 'restore-150' $engine @('-NoProfile','-File',$driver,'-Action','Select','-Scale','150') '' $runCeiling 60000
            $context.RestoreExit=$restored.ExitCode
            if ($context.RestoreExit -ne 0) { throw 'WRI-RESTORE: UIA restore failed' }
        } -Readback {
            $readback=Invoke-WriScaleRecordedChild $context 'restore-readback' $toolchain.Dotnet @('exec',$dll,'--scale-diagnostic') '' $runCeiling 120000 BuildVerifier
            $context.ReadbackExit=$readback.ExitCode
            if ($context.ReadbackExit -ne 0) { throw 'WRI-RESTORE: fresh diagnostic failed' }
            Assert-WriScaleContext $readback.Stdout 'scale-diagnostic' 1.5
        }
    } catch { $failure=$_.Exception.GetType().Name }
    finally {
        try { Assert-WriSourceUnchanged $repo $baseline $clock $runCeiling }
        catch { $failure='SourceGuardFailure' }
        $receipt=@{BaseFingerprint=$baseline;SourceUnchanged=($failure -ne 'SourceGuardFailure');FailureType=$failure;ContractFailed=$context.ContractFailed;RestoreExit=$context.RestoreExit;ReadbackExit=$context.ReadbackExit;RunCeilingMs=$runCeiling;ExecutionDeadlineMs=$executionDeadline;Records=$context.Records;ElapsedMs=$clock.ElapsedMilliseconds}
        # Apply the same home/hostname/SID boundary to metadata as to child text, before publication.
        $raw=Join-Path ([IO.Path]::GetTempPath()) ('cfd-scale-ledger-'+[guid]::NewGuid().ToString('N')+'.json')
        try {
            [IO.File]::WriteAllText($raw,(@{stdout=($receipt | ConvertTo-Json -Depth 12 -Compress);stderr=''} | ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
            $safe=Invoke-WriProcess -Repo $repo -Exe $toolchain.Python -Arguments @((Join-Path $PSScriptRoot 'check-windows-runner.py'),'--scrub-capture',$raw) -Clock $clock -CeilingMs $runCeiling
            Assert-WriNumericExit $safe.ExitCode
            if ($safe.ExitCode -ne 0) { throw 'WRI-CAPTURE: ledger PHN publication withheld' }
            $derivative=$safe.Stdout | ConvertFrom-Json
            [IO.File]::WriteAllText((Join-Path $output 'run.json'),$derivative.stdout,[Text.UTF8Encoding]::new($false))
            [IO.File]::WriteAllText((Join-Path $output 'publication.json'),($derivative.substitutions | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
        } finally { Remove-Item -LiteralPath $raw -Force }
        Assert-WriEnvelope $clock $runCeiling
    }
    if ($failure -or $context.ContractFailed -or $context.RestoreExit -ne 0 -or $context.ReadbackExit -ne 0) { throw 'WRI-RUN: blocked or failed; retained receipt is not readiness' }
    'WRI-RUN PASS scale-contract-only readiness=false'
}
if ($MyInvocation.InvocationName -eq '.') { return }
if (-not $IsWindows -or $PSVersionTable.PSVersion.Major -lt 7) { throw 'WRI-PLATFORM: PowerShell 7 on Windows required' }
if ($Action -eq 'Select') { Select-WriScale $Scale }
elseif ($Action -eq 'Run') { Invoke-WriScaleRun $PythonPath $OutputDirectory }
else { throw 'WRI-SCALE: explicit Run or Select action required' }
