
# DotNetCorePatchNotifier - Windows startup task
# Run PowerShell as Administrator.
#
# Usage:
# .\Install-AutoStart.ps1 -ExePath "C:\Apps\DotNetCorePatchNotifier\DotNetCorePatchNotifier.exe"

param(
    [Parameter(Mandatory = $true)]
    [string]$ExePath
)

$ErrorActionPreference = "Stop"
$TaskName = "DotNetCorePatchNotifier"

# Validate executable path.
$ExePath = [System.IO.Path]::GetFullPath($ExePath)

if (-not (Test-Path -LiteralPath $ExePath -PathType Leaf)) {
    throw "Executable not found: $ExePath"
}

$WorkingDirectory = Split-Path -Path $ExePath -Parent

# Run the application at Windows startup.
$Action = New-ScheduledTaskAction `
    -Execute $ExePath `
    -WorkingDirectory $WorkingDirectory

$Trigger = New-ScheduledTaskTrigger -AtStartup

$Settings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -MultipleInstances IgnoreNew `
    -StartWhenAvailable

# Run under the current account, without storing a password.
# S4U does not create an interactive desktop session.
$User = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name

$Principal = New-ScheduledTaskPrincipal `
    -UserId $User `
    -LogonType S4U `
    -RunLevel Limited

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $Action `
    -Trigger $Trigger `
    -Settings $Settings `
    -Principal $Principal `
    -Description "Starts DotNetCorePatchNotifier when Windows starts." `
    -Force

Write-Host "Task installed: $TaskName" -ForegroundColor Green
Write-Host "Executable: $ExePath"
Write-Host "Trigger: At system startup"
Write-Host ""
Write-Warning "MAUI UI and tray icon will not be visible before interactive logon."
Write-Warning "S4U tasks may have restricted access to encrypted user credentials."
