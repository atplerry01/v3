# Whycespace Build Script (PowerShell)
param(
    [string]$Configuration = "Release",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$SolutionPath = Join-Path $PSScriptRoot "../../Whycespace.slnx"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Whycespace WBSM v3 Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/4] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $SolutionPath
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

Write-Host "[2/4] Building solution ($Configuration)..." -ForegroundColor Yellow
dotnet build $SolutionPath --no-restore --configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

if (-not $SkipTests) {
    Write-Host "[3/4] Running tests..." -ForegroundColor Yellow
    dotnet test $SolutionPath --no-build --configuration $Configuration --logger "trx;LogFileName=test-results.trx" --results-directory ./test-results
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
} else {
    Write-Host "[3/4] Skipping tests" -ForegroundColor DarkGray
}

Write-Host "[4/4] Publishing platform..." -ForegroundColor Yellow
$PlatformProject = Join-Path $PSScriptRoot "../../src/platform/Whycespace.Platform.csproj"
dotnet publish $PlatformProject --no-build --configuration $Configuration --output ./artifacts/platform
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Build succeeded" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
