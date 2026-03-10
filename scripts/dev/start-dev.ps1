# Whycespace Local Development Startup (PowerShell)
param(
    [switch]$InfraOnly,
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"
$ComposeFile = Join-Path $PSScriptRoot "../../infrastructure/localdev/docker-compose.yml"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Whycespace Local Dev Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Starting infrastructure services..." -ForegroundColor Yellow
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
    Write-Host "Starting Whycespace Platform..." -ForegroundColor Yellow
    $PlatformProject = Join-Path $PSScriptRoot "../../src/platform/Whycespace.Platform.csproj"
    dotnet run --project $PlatformProject
}
