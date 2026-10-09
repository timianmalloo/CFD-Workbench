# Planted rejected orchestration shape; no real scale or product action.
function Invoke-WriScaleSequence {
    param([scriptblock]$Preflight,[scriptblock]$Build,[scriptblock]$Select,
          [scriptblock]$Contract,[scriptblock]$Restore,[scriptblock]$Readback)
    & $Preflight '150% (Recommended)'
    & $Build
    foreach ($scale in @(150,200)) { & $Select $scale; & $Contract $scale }
}
