# Whycespace Local Development Startup (PowerShell)
param(
    [switch]$InfraOnly,
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"
Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

$SolutionRoot = Get-SolutionRoot
$ComposeFile = Join-Path $SolutionRoot "infrastructure/localdev/docker-compose.yml"
$PlatformProject = Get-PlatformProject

Write-Banner "Whycespace Local Dev Environment"

Write-Step 1 2 "Starting infrastructure services..."
$composeArgs = @("-f", $ComposeFile, "up", "-d")
if ($Rebuild) { $composeArgs += "--build" }
docker compose @composeArgs

Write-Host ""
Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "Services:" -ForegroundColor Green
Write-Host "  Kafka:      localhost:29092" -ForegroundColor White
Write-Host "  Postgres:   localhost:5432  (whyce/whyce_dev)" -ForegroundColor White
Write-Host "  Redis:      localhost:6379" -ForegroundColor White
Write-Host "  Prometheus: http://localhost:9090" -ForegroundColor White
Write-Host "  Grafana:    http://localhost:3000 (admin/whyce_dev)" -ForegroundColor White
Write-Host ""

if (-not $InfraOnly) {
    Write-Step 2 2 "Starting Whycespace Platform..."
    dotnet run --project $PlatformProject
} else {
    Write-Success "Infrastructure ready. Platform not started (-InfraOnly)."
}
