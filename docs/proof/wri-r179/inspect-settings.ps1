$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root=[System.Windows.Automation.AutomationElement]::RootElement
$wins=$root.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)
foreach($w in $wins){
 if($w.Current.Name -ne 'Settings'){continue}
 "FRAME name=$($w.Current.Name) id=$($w.Current.AutomationId) class=$($w.Current.ClassName)"
 $all=$w.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
 foreach($e in $all){
  if($e.Current.AutomationId -match 'Display|Scaling|Frame|Content' -or $e.Current.Name -match 'Multiple displays|Show more|150%|200%|Display settings'){
   $par=[System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($e)
   "NODE name=$($e.Current.Name) id=$($e.Current.AutomationId) type=$($e.Current.ControlType.ProgrammaticName) offscreen=$($e.Current.IsOffscreen) parent=$($par.Current.Name) parentid=$($par.Current.AutomationId) patterns=$([string]::Join(',',@($e.GetSupportedPatterns()|ForEach-Object ProgrammaticName)))"
   $p=$null
   if($e.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$p)){"SELECTED=$($p.Current.IsSelected)"}
  }
 }
}
