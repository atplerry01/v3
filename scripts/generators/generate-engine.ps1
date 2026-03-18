# Whycespace Engine Generator
param(
    [Parameter(Mandatory)]
    [string]$Name,

    [Parameter(Mandatory)]
    [ValidateSet("T0U_Constitutional", "T1M_Orchestration", "T2E_Execution", "T3I_Intelligence", "T4A_Access")]
    [string]$Tier
)

$ErrorActionPreference = "Stop"
$EnginesDir = Join-Path $PSScriptRoot "../../src/engines/$Tier"

if (-not (Test-Path $EnginesDir)) {
    New-Item -ItemType Directory -Path $EnginesDir -Force | Out-Null
}

$FileName = "${Name}Engine.cs"
$FilePath = Join-Path $EnginesDir $FileName

$TierNamespace = $Tier -replace "_", "_"

$Content = @"
namespace Whycespace.Engines.$TierNamespace;

using Whycespace.Shared.Contracts;

public sealed class ${Name}Engine : IEngine
{
    public string Name => "$Name";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // TODO: Implement engine logic
        var events = new[]
        {
            EngineEvent.Create("${Name}Completed", Guid.Parse(context.WorkflowId), context.Data)
        };

        return Task.FromResult(EngineResult.Ok(events));
    }
}
"@

Set-Content -Path $FilePath -Value $Content -Encoding UTF8

Write-Host "Engine created: $FilePath" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Implement engine logic in ExecuteAsync"
Write-Host "  2. Register engine in src/platform/Program.cs"
Write-Host "  3. Add tests in tests/engines/${Name}EngineTests.cs"
