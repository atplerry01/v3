# Whycespace Simulation Runner
# Executes load simulation against the WBSM v3 runtime.
param(
    [ValidateSet("small", "medium", "large")]
    [string]$Scenario = "small",

    [int]$Workers = 10,

    [int]$DurationSec = 0,

    [double]$FaultRate = 0.0,

    [string]$OutputPath = "",

    [switch]$Atlas,

    [switch]$BuildFirst
)

$ErrorActionPreference = "Stop"
Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

Write-Banner "Whycespace Simulation Runner"

$SolutionRoot = Get-SolutionRoot
$SimProject = Get-SimulationProject

if (-not (Test-Path $SimProject)) {
    Write-Failure "Simulation project not found at: $SimProject"
    exit 1
}

if ($BuildFirst) {
    Write-Step 1 3 "Building simulation project..."
    dotnet build $SimProject --configuration Release --verbosity quiet
    Assert-ExitCode "Build"
} else {
    Write-Step 1 3 "Skipping build (use -BuildFirst to compile)"
}

Write-Host ""
Write-Step 2 3 "Configuring simulation..."
Write-Host "  Scenario:    $Scenario" -ForegroundColor White
Write-Host "  Workers:     $Workers" -ForegroundColor White
Write-Host "  Fault rate:  $($FaultRate * 100)%" -ForegroundColor White
if ($DurationSec -gt 0) { Write-Host "  Duration:    ${DurationSec}s" -ForegroundColor White }
if ($Atlas) { Write-Host "  Atlas:       enabled" -ForegroundColor White }
if ($OutputPath) { Write-Host "  Output:      $OutputPath" -ForegroundColor White }
Write-Host ""

$runArgs = @("--scenario", $Scenario, "--workers", $Workers, "--fault-rate", $FaultRate)
if ($DurationSec -gt 0) { $runArgs += @("--duration", $DurationSec) }
if ($OutputPath) { $runArgs += @("--output", $OutputPath) }

Write-Step 3 3 "Running simulation..."
dotnet run --project $SimProject --configuration Release -- @runArgs
Assert-ExitCode "Simulation"

Write-Host ""
Write-Success "Simulation complete."
