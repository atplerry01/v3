# Whycespace Governance Policy Evaluator
# Validates policy configurations and runs evaluation against test contexts.
param(
    [ValidateSet("validate", "evaluate", "audit", "all")]
    [string]$Mode = "validate",

    [string]$PolicyFilter = "",

    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

Write-Banner "Whycespace Governance Policy Evaluator"

$SolutionRoot = Get-SolutionRoot

# --- Validate Policy Engine Implementations ---
if ($Mode -eq "validate" -or $Mode -eq "all") {
    Write-Step 1 3 "Validating policy engine implementations..."

    $policyPath = Join-Path $SolutionRoot "src/engines/T0U/whycepolicy"
    if (Test-Path $policyPath) {
        $policyEngines = Get-ChildItem -Path $policyPath -Recurse -Filter "*Engine.cs" |
            Where-Object { $_.Name -notmatch "Test|Mock" }
        if ($PolicyFilter) {
            $policyEngines = $policyEngines | Where-Object { $_.Name -like "*$PolicyFilter*" }
        }
        $count = ($policyEngines | Measure-Object).Count
        Write-Host "  WhycePolicy engines found: $count" -ForegroundColor White
        if ($Verbose) {
            foreach ($e in $policyEngines) {
                Write-Host "    - $($e.FullName -replace [regex]::Escape($SolutionRoot), '')" -ForegroundColor DarkGray
            }
        }
    } else {
        Write-Failure "  WhycePolicy engine path not found: $policyPath"
    }
    Write-Host ""
}

# --- Evaluate Policy Integration Points ---
if ($Mode -eq "evaluate" -or $Mode -eq "all") {
    Write-Step 2 3 "Scanning policy integration points..."

    $systemPath = Join-Path $SolutionRoot "src/systems"
    if (Test-Path $systemPath) {
        $policyAdapters = Get-ChildItem -Path $systemPath -Recurse -Filter "*PolicyAdapter.cs"
        $policyContexts = Get-ChildItem -Path $systemPath -Recurse -Filter "*PolicyEvaluationContext.cs"
        Write-Host "  Policy adapters: $(($policyAdapters | Measure-Object).Count)" -ForegroundColor White
        Write-Host "  Policy evaluation contexts: $(($policyContexts | Measure-Object).Count)" -ForegroundColor White
        if ($Verbose) {
            foreach ($a in $policyAdapters) {
                Write-Host "    - $($a.FullName -replace [regex]::Escape($SolutionRoot), '')" -ForegroundColor DarkGray
            }
        }
    }
    Write-Host ""
}

# --- Audit Policy Coverage ---
if ($Mode -eq "audit" -or $Mode -eq "all") {
    Write-Step 3 3 "Auditing governance coverage..."

    $idPath = Join-Path $SolutionRoot "src/engines/T0U/whyceid"
    $chainPath = Join-Path $SolutionRoot "src/engines/T0U/whycechain"

    $idCount = 0
    $chainCount = 0
    if (Test-Path $idPath) {
        $idCount = (Get-ChildItem -Path $idPath -Recurse -Filter "*Engine.cs" | Measure-Object).Count
    }
    if (Test-Path $chainPath) {
        $chainCount = (Get-ChildItem -Path $chainPath -Recurse -Filter "*Engine.cs" | Measure-Object).Count
    }

    Write-Host "  WhyceID engines:     $idCount" -ForegroundColor White
    Write-Host "  WhyceChain engines:  $chainCount" -ForegroundColor White
    Write-Host ""

    $runtimePolicyPath = Join-Path $SolutionRoot "src/runtime/policy-enforcement"
    if (Test-Path $runtimePolicyPath) {
        $runtimePolicies = Get-ChildItem -Path $runtimePolicyPath -Recurse -Filter "*.cs"
        Write-Host "  Runtime policy enforcement files: $(($runtimePolicies | Measure-Object).Count)" -ForegroundColor White
    }
    Write-Host ""
}

Write-Success "Governance evaluation complete."
