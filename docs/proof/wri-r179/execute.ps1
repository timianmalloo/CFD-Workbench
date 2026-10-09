$ErrorActionPreference='Stop'
$dotnet=Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
$dll='tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll'
$proof='docs/proof/wri-r179'
$groups=@(
 @{mode='shell-window';names='KeyBindings_MenuGesture_NotBound'},
 @{mode='views';names='ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView,ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack,Elevation_SideSelectedStation_RenderedFullWeight,View3d_SelectedStation_RenderedWidthAndChip,ModelArea_Views_SeparatedByGutterAndFramed,View3d_ChipBorderSampler_FindsStationColourInTheLoggedWindowsBlock,Elevation_ChipBorderSampler_HoldsHalfOfASplitLine,View3d_CubeFocusRing_GapOnCurrentFaceThreeToOne'},
 @{mode='properties-cells';names='PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs,PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused,PropertiesPane_Density_EveryTargetAtLeast24,PropertiesPane_Density_EveryInputDeclaresMinHeightOf24'},
 @{mode='analysis';names='Analysis_FourViews_At1280x800_AndGeometryUnchangedAt1500x870'}
)
$hashes=Get-ChildItem 'tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0' -Filter 'CfdWorkbench*.dll'|ForEach-Object {@{path=$_.FullName.Replace((Get-Location).Path+'\','').Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;bytes=$_.Length}}
[IO.File]::WriteAllText((Join-Path $proof 'dll-hashes.json'),($hashes|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
try {
 foreach($scale in @('150','200')){
  & "$proof/process.ps1" -Label "settings-$scale" -Exe powershell -Arguments @('-NoProfile','-ExecutionPolicy','Bypass','-File',"$proof/settings.ps1",'-Action',$scale)
  $meta=Get-Content "$proof/settings-$scale.measurement.json"|ConvertFrom-Json
  if($meta.ProcessExitCode -ne 0){throw "Settings $scale failed"}
  foreach($g in $groups){
   if([DateTime]::UtcNow -gt [DateTime]::Parse('2026-10-09T04:20:00Z')){throw 'Execution cutoff leaves three minutes for restore and package'}
   & "$proof/process.ps1" -Label "$scale-$($g.mode)" -Exe $dotnet -Arguments @('exec',$dll,"--$($g.mode)") -Only $g.names -Affinity
  }
 }
} finally {
 & "$proof/process.ps1" -Label 'settings-restored' -Exe powershell -Arguments @('-NoProfile','-ExecutionPolicy','Bypass','-File',"$proof/settings.ps1",'-Action','150')
 & "$proof/process.ps1" -Label 'restoration-diagnostic' -Exe $dotnet -Arguments @('exec',$dll,'--r179-diagnostic') -TimeoutSeconds 25 -Affinity
}
