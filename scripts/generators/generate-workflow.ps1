# Whycespace Workflow Generator
param(
    [Parameter(Mandatory)]
    [string]$Name,

    [Parameter(Mandatory)]
    [string[]]$Steps
)

$ErrorActionPreference = "Stop"
Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

Write-Banner "Whycespace Workflow Generator"

$SolutionRoot = Get-SolutionRoot
$WorkflowsDir = Join-Path $SolutionRoot "src/systems/midstream/WSS/workflows"

if (-not (Test-Path $WorkflowsDir)) {
    New-Item -ItemType Directory -Path $WorkflowsDir -Force | Out-Null
}

$FileName = "${Name}Workflow.cs"
$FilePath = Join-Path $WorkflowsDir $FileName

$StepDefinitions = @()
for ($i = 0; $i -lt $Steps.Count; $i++) {
    $step = $Steps[$i]
    $stepId = $step.ToLower() -replace " ", "-"
    $engineName = $step -replace " ", ""
    $nextSteps = if ($i -lt $Steps.Count - 1) {
        $nextId = ($Steps[$i + 1].ToLower() -replace " ", "-")
        "new[] { `"$nextId`" }"
    } else {
        "Array.Empty<string>()"
    }
    $StepDefinitions += "            new(`"$stepId`", `"$step`", `"$engineName`", $nextSteps)"
}

$StepsList = $StepDefinitions -join ",`n"

$Content = @"
namespace Whycespace.Systems.Midstream.WSS.Workflows;

using Whycespace.Contracts.Workflows;

public sealed class ${Name}Workflow : IWorkflowDefinition
{
    public string WorkflowName => "$Name";

    public WorkflowGraph BuildGraph()
    {
        var steps = new List<WorkflowStep>
        {
$StepsList
        };

        return new WorkflowGraph(Guid.NewGuid().ToString(), WorkflowName, steps);
    }
}
"@

Set-Content -Path $FilePath -Value $Content -Encoding UTF8

Write-Success "Workflow created: $FilePath"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Register workflow in WSS WorkflowMapper"
Write-Host "  2. Map command to workflow in WorkflowRouter"
Write-Host "  3. Add tests in tests/workflows/${Name}WorkflowTests.cs"
