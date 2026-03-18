# Whycespace Build Script (PowerShell)
param(
    [string]$Configuration = "Release",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

$SolutionPath = Get-SolutionPath
$PlatformProject = Get-PlatformProject

Write-Banner "Whycespace WBSM v3 Build"

Write-Step 1 4 "Restoring dependencies..."
dotnet restore $SolutionPath
Assert-ExitCode "Restore"

Write-Step 2 4 "Building solution ($Configuration)..."
dotnet build $SolutionPath --no-restore --configuration $Configuration
Assert-ExitCode "Build"

if (-not $SkipTests) {
    Write-Step 3 4 "Running tests..."
    dotnet test $SolutionPath --no-build --configuration $Configuration --logger "trx;LogFileName=test-results.trx" --results-directory ./test-results
    Assert-ExitCode "Tests"
} else {
    Write-Host "[3/4] Skipping tests" -ForegroundColor DarkGray
}

Write-Step 4 4 "Publishing platform..."
dotnet publish $PlatformProject --no-build --configuration $Configuration --output ./artifacts/platform
Assert-ExitCode "Publish"

Write-Host ""
Write-Success "Build succeeded."
