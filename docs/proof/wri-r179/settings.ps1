param([ValidateSet('Preflight','Verify','150','200')][string]$Action='Preflight')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$AE=[System.Windows.Automation.AutomationElement]
$TS=[System.Windows.Automation.TreeScope]
function Frame {
 $ws=$AE::RootElement.FindAll($TS::Children,[System.Windows.Automation.Condition]::TrueCondition)
 foreach($x in $ws){if($x.Current.Name -eq 'Settings' -and $x.Current.ClassName -eq 'ApplicationFrameWindow'){return $x}}
 throw 'Exact Settings frame absent'
}
function Id($id){
 $w=Frame
 $c=[System.Windows.Automation.PropertyCondition]::new($AE::AutomationIdProperty,$id)
 $e=$w.FindFirst($TS::Descendants,$c)
 if(!$e){throw "Exact control absent: $id"}; return $e
}
function Describe($label,$e){
 "${label} name=$($e.Current.Name) id=$($e.Current.AutomationId) type=$($e.Current.ControlType.ProgrammaticName) enabled=$($e.Current.IsEnabled) offscreen=$($e.Current.IsOffscreen) patterns=$([string]::Join(',',@($e.GetSupportedPatterns()|ForEach-Object ProgrammaticName)))"
}
$frame=Frame; Describe 'FRAME' $frame
$d=Id 'Display1'; Describe 'DISPLAY' $d
$sel=$d.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
"Display1 selected=$($sel.Current.IsSelected)";if(!$sel.Current.IsSelected){throw 'Display1 is not selected'}
$all=$frame.FindAll($TS::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
$group=$null
foreach($e in $all){if($e.Current.Name -eq 'Multiple displays' -and $e.Current.ControlType -eq [System.Windows.Automation.ControlType]::Group){$group=$e;break}}
if(!$group){throw 'Multiple displays Group absent'}
$more=$group.FindFirst($TS::Children,[System.Windows.Automation.PropertyCondition]::new($AE::AutomationIdProperty,'EntityItemButton'))
if(!$more){throw 'Multiple displays child Show-more absent'}
Describe 'MULTIPLE_GROUP' $group; Describe 'SHOW_MORE' $more
$expand=$more.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
"SHOW_MORE before=$($expand.Current.ExpandCollapseState)"
if($expand.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Collapsed){$expand.Expand();Start-Sleep -Milliseconds 350}
$main=Id 'SystemSettings_Display_MainMonitor_CheckBox';Describe 'MAIN_MONITOR' $main
$toggle=$main.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
"MAIN_MONITOR toggle=$($toggle.Current.ToggleState)";if($toggle.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On){throw 'Display1 is not main monitor'}
$combo=Id 'SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox'
$scroll=$combo.GetCurrentPattern([System.Windows.Automation.ScrollItemPattern]::Pattern);$scroll.ScrollIntoView();Start-Sleep -Milliseconds 350
$combo=Id 'SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox';Describe 'SCALE_REACQUIRED' $combo
$container=$combo.GetCurrentPattern([System.Windows.Automation.ItemContainerPattern]::Pattern)
foreach($name in @('150% (Recommended)','200%')){
 $item=$container.FindItemByProperty($null,$AE::NameProperty,$name)
 if(!$item){throw "Scale item absent: $name"}
 $vp=$null
 if($item.TryGetCurrentPattern([System.Windows.Automation.VirtualizedItemPattern]::Pattern,[ref]$vp)){$vp.Realize();"REALIZED $name"}
 $combo=Id 'SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox'
 $container=$combo.GetCurrentPattern([System.Windows.Automation.ItemContainerPattern]::Pattern)
 $item=$container.FindItemByProperty($null,$AE::NameProperty,$name)
 Describe 'SCALE_ITEM' $item
 $sp=$item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
 "SCALE_ITEM name=$name selected=$($sp.Current.IsSelected)"
}
if($Action -in @('150','200')){
 $wanted=if($Action -eq '150'){'150% (Recommended)'}else{'200%'}
 $combo=Id 'SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox'
 $item=$combo.GetCurrentPattern([System.Windows.Automation.ItemContainerPattern]::Pattern).FindItemByProperty($null,$AE::NameProperty,$wanted)
 $vp=$null;if($item.TryGetCurrentPattern([System.Windows.Automation.VirtualizedItemPattern]::Pattern,[ref]$vp)){$vp.Realize()}
 $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
 Start-Sleep -Milliseconds 1000
}
$combo=Id 'SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox'
$selected=$combo.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection()
$actual=[string]::Join(',',@($selected|ForEach-Object {$_.Current.Name}))
"SETTINGS_SELECTED=$actual"
if($Action -in @('150','200') -and $actual -ne $wanted){throw "Selected $actual instead of $wanted"}
if($Action -eq 'Preflight' -and $actual -ne '150% (Recommended)'){throw "Preflight requires exact 150: $actual"}
"SETTINGS_SUCCESS action=$Action UTC=$([DateTime]::UtcNow.ToString('o'))"
