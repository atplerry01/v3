# Whycespace Shared Script Helpers
# Import: Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

function Write-Step {
    param(
        [int]$Step,
        [int]$Total,
        [string]$Message
    )
    Write-Host "[$Step/$Total] $Message" -ForegroundColor Yellow
}

function Write-Banner {
    param([string]$Title)
    $divider = "=" * 48
    Write-Host $divider -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host $divider -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Red
}

function Get-SolutionRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "../../")).Path
}

function Get-SolutionPath {
    return Join-Path (Get-SolutionRoot) "Whycespace.slnx"
}

function Get-PlatformProject {
    return Join-Path (Get-SolutionRoot) "src/platform/Whycespace.Platform.csproj"
}

function Get-SimulationProject {
    return Join-Path (Get-SolutionRoot) "simulation/Whycespace.Simulation/Whycespace.Simulation.csproj"
}

function Get-FoundationHostProject {
    return Join-Path (Get-SolutionRoot) "infrastructure/host/Whycespace.FoundationHost/Whycespace.FoundationHost.csproj"
}

function Assert-ExitCode {
    param([string]$Operation)
    if ($LASTEXITCODE -ne 0) { throw "$Operation failed with exit code $LASTEXITCODE" }
}

Export-ModuleMember -Function *
