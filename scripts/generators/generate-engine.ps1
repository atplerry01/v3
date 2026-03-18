# Whycespace Engine Generator
param(
    [Parameter(Mandatory)]
    [string]$Name,

    [Parameter(Mandatory)]
    [ValidateSet("T0U", "T1M", "T2E", "T3I", "T4A")]
    [string]$Tier,

    [string]$Subdomain = ""
)

$ErrorActionPreference = "Stop"
Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

Write-Banner "Whycespace Engine Generator"

$SolutionRoot = Get-SolutionRoot
$EnginesDir = if ($Subdomain) {
    Join-Path $SolutionRoot "src/engines/$Tier/$Subdomain"
} else {
    Join-Path $SolutionRoot "src/engines/$Tier"
}

if (-not (Test-Path $EnginesDir)) {
    New-Item -ItemType Directory -Path $EnginesDir -Force | Out-Null
}

$FileName = "${Name}Engine.cs"
$FilePath = Join-Path $EnginesDir $FileName

$TierLabel = switch ($Tier) {
    "T0U" { "Constitutional" }
    "T1M" { "Orchestration" }
    "T2E" { "Execution" }
    "T3I" { "Intelligence" }
    "T4A" { "Access" }
}

$Namespace = "Whycespace.Engines.$Tier"
if ($Subdomain) { $Namespace += ".$Subdomain" }

$Content = @"
namespace $Namespace;

using Whycespace.Contracts.Engines;

/// <summary>
/// $TierLabel-tier engine: $Name.
/// </summary>
public sealed class ${Name}Engine : IEngine
{
    public string Name => "$Name";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // TODO: Implement engine logic
        var events = new[]
        {
            EngineEvent.Create("${Name}Completed", context.WorkflowId, context.Data)
        };

        return Task.FromResult(EngineResult.Ok(events));
    }
}
"@

Set-Content -Path $FilePath -Value $Content -Encoding UTF8

Write-Success "Engine created: $FilePath"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Implement engine logic in ExecuteAsync"
Write-Host "  2. Register engine in EngineRegistry"
Write-Host "  3. Add tests in tests/engines/${Name}EngineTests.cs"
