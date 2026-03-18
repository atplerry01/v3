# Whycespace Event Replay
# Replays events from the event store to rebuild projections or workflow state.
param(
    [Parameter(Mandatory)]
    [ValidateSet("projections", "workflows", "all")]
    [string]$Target,

    [string]$FromTimestamp = "",

    [string]$ToTimestamp = "",

    [string]$AggregateId = "",

    [int]$BatchSize = 500,

    [switch]$DryRun,

    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

Write-Banner "Whycespace Event Replay"

$SolutionRoot = Get-SolutionRoot
$PlatformProject = Get-PlatformProject

if ($DryRun) {
    Write-Host "[DRY RUN] No changes will be applied." -ForegroundColor Magenta
    Write-Host ""
}

# Build arguments for the replay controller
$replayArgs = @("--replay-target", $Target, "--batch-size", $BatchSize)

if ($FromTimestamp) { $replayArgs += @("--from", $FromTimestamp) }
if ($ToTimestamp) { $replayArgs += @("--to", $ToTimestamp) }
if ($AggregateId) { $replayArgs += @("--aggregate", $AggregateId) }
if ($DryRun) { $replayArgs += "--dry-run" }

Write-Step 1 3 "Validating event store connectivity..."
Write-Host "  Target: $Target" -ForegroundColor White
Write-Host "  Batch size: $BatchSize" -ForegroundColor White
if ($FromTimestamp) { Write-Host "  From: $FromTimestamp" -ForegroundColor White }
if ($ToTimestamp) { Write-Host "  To: $ToTimestamp" -ForegroundColor White }
if ($AggregateId) { Write-Host "  Aggregate: $AggregateId" -ForegroundColor White }
Write-Host ""

Write-Step 2 3 "Building replay runtime..."
dotnet build $PlatformProject --configuration Release --verbosity quiet
Assert-ExitCode "Build"
Write-Host ""

Write-Step 3 3 "Executing event replay..."
Write-Host "  Running: dotnet run --project $PlatformProject -- replay $($replayArgs -join ' ')" -ForegroundColor DarkGray

if (-not $DryRun) {
    dotnet run --project $PlatformProject --configuration Release -- replay @replayArgs
    Assert-ExitCode "Replay"
    Write-Success "Event replay completed successfully."
} else {
    Write-Host ""
    Write-Success "[DRY RUN] Replay plan generated. No events were replayed."
}
