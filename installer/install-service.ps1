param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
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

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$serviceProject = Join-Path $repoRoot "src\Dyndns.Service\Dyndns.Service.csproj"
$publishFolder = if ([string]::IsNullOrWhiteSpace($PublishFolder)) {
    Join-Path $scriptRoot "publish"
} else {
    $PublishFolder
}

New-Item -ItemType Directory -Force -Path $publishFolder | Out-Null

dotnet publish $serviceProject -c $Configuration -r $Runtime --self-contained false -o $publishFolder

$serviceName = "Dynv6 Automaton"
$serviceExe = Join-Path $publishFolder "Dyndns.Service.exe"

if (-not (Test-Path $serviceExe)) {
    throw "Publish output not found: $serviceExe"
}

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName | Out-Null
}

sc.exe create $serviceName binPath= "`"$serviceExe`"" start= auto obj= LocalSystem DisplayName= $serviceName | Out-Null
sc.exe description $serviceName "Dynv6 DNS updater with local dashboard." | Out-Null
sc.exe failure $serviceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
sc.exe failureflag $serviceName 1 | Out-Null
sc.exe config $serviceName start= delayed-auto | Out-Null

Start-Service -Name $serviceName

Write-Host "Installed service '$serviceName' and started it."
Write-Host "Dashboard: http://localhost:5050"