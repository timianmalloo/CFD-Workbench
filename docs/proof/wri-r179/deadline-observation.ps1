$ErrorActionPreference='Stop'
$parsed=[DateTime]::Parse('2026-10-09T04:20:00Z')
$now=[DateTime]::UtcNow
"UTC_NOW=$($now.ToString('o')) Kind=$($now.Kind)"
"PARSED_DEADLINE=$($parsed.ToString('o')) Kind=$($parsed.Kind)"
"DIRECT_COMPARISON_EXPIRED=$($now -gt $parsed)"
"NORMALIZED_UTC_DEADLINE=$($parsed.ToUniversalTime().ToString('o'))"
"NORMALIZED_COMPARISON_EXPIRED=$($now -gt $parsed.ToUniversalTime())"
"LOCAL_TIMEZONE=$([TimeZoneInfo]::Local.Id)"
