# Read-only W-3 host survey; no privileged feature or registry mutation.
$cpu = Get-CimInstance Win32_Processor
$machine = Get-CimInstance Win32_ComputerSystem
$os = Get-CimInstance Win32_OperatingSystem
$version = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
$drive = Get-PSDrive -Name C
[ordered]@{
    os_caption = $os.Caption
    os_version = $os.Version
    build = $version.CurrentBuildNumber
    ubr = $version.UBR
    cpu = $cpu.Name
    physical_cores = $cpu.NumberOfCores
    logical_processors = $cpu.NumberOfLogicalProcessors
    ram_bytes = $machine.TotalPhysicalMemory
    free_c_bytes = $drive.Free
    virtualization_firmware_enabled = $cpu.VirtualizationFirmwareEnabled
    hypervisor_present = $machine.HypervisorPresent
    architecture = $env:PROCESSOR_ARCHITECTURE
    manufacturer = $machine.Manufacturer
    reboot_cbs_key = Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending'
    reboot_update_key = Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired'
} | ConvertTo-Json
