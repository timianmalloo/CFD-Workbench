# Ring: Windows runner preparation. Read-only UIA. Ceiling: runner's 60 s child limit.
param([ValidateSet('Preflight')][string]$Action='Preflight', [switch]$SelfTestAbsentFrame)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'windows-runner.ps1')
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$ae=[System.Windows.Automation.AutomationElement]
$ts=[System.Windows.Automation.TreeScope]
$frame=$null
$windows = if ($SelfTestAbsentFrame) { @() } else { $ae::RootElement.FindAll($ts::Children,[System.Windows.Automation.Condition]::TrueCondition) }
foreach ($window in $windows) {
    if ($window.Current.Name -eq 'Settings' -and $window.Current.ClassName -eq 'ApplicationFrameWindow') { $frame=$window; break }
}
if (-not $frame) { throw 'WRI-PREFLIGHT: exact Settings frame absent; prepare Settings manually' }
function Find-Required([string]$Id) {
    $control=$frame.FindFirst($ts::Descendants,[System.Windows.Automation.PropertyCondition]::new($ae::AutomationIdProperty,$Id))
    if (-not $control) { throw "WRI-PREFLIGHT: required control absent: $Id" }
    return $control
}
$display=Find-Required 'Display1'
$selected=$display.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected
$group=$null
foreach ($element in $frame.FindAll($ts::Descendants,[System.Windows.Automation.Condition]::TrueCondition)) {
    if ($element.Current.Name -eq 'Multiple displays' -and $element.Current.ControlType -eq [System.Windows.Automation.ControlType]::Group) { $group=$element; break }
}
if (-not $group) { throw 'WRI-PREFLIGHT: Multiple displays group absent' }
$more=$group.FindFirst($ts::Children,[System.Windows.Automation.PropertyCondition]::new($ae::AutomationIdProperty,'EntityItemButton'))
if (-not $more) { throw 'WRI-PREFLIGHT: show-more control absent' }
$expanded=$more.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Expanded
if (-not $expanded) { throw 'WRI-PREFLIGHT: Multiple displays is collapsed; prepare Settings manually' }
$main=Find-Required 'SystemSettings_Display_MainMonitor_CheckBox'
$isMain=$main.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
$combo=Find-Required 'SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox'
$selection=$combo.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection()
$scale=[string]::Join(',',@($selection | ForEach-Object { $_.Current.Name }))
Assert-WriSettingsState ([pscustomobject]@{Frame=$true;Selected=$selected;Expanded=$expanded;Main=$isMain;Visible=($combo.Current.IsEnabled -and -not $combo.Current.IsOffscreen);Scale=$scale})
"SETTINGS_SUCCESS action=$Action selected=$scale read_only=true"
