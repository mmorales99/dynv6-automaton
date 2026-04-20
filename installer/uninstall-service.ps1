param(
    [string]$PublishFolder = ""
)

$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell session."
    }
}

Assert-Administrator

$serviceName = "Dynv6 Automaton"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishFolder = if ([string]::IsNullOrWhiteSpace($PublishFolder)) {
    Join-Path $scriptRoot "publish"
} else {
    $PublishFolder
}

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName | Out-Null
}

if (Test-Path $publishFolder) {
    Remove-Item -Recurse -Force $publishFolder
}

Write-Host "Removed service '$serviceName'."