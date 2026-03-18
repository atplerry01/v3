# Whycespace Runtime Inspector
# Inspects runtime engine registry, workflow state, and partition health.
param(
    [ValidateSet("engines", "workflows", "partitions", "all")]
    [string]$Target = "all",

    [string]$EngineFilter = "",

    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

Write-Banner "Whycespace Runtime Inspector"

$SolutionRoot = Get-SolutionRoot

# --- Engine Registry ---
if ($Target -eq "engines" -or $Target -eq "all") {
    Write-Step 1 3 "Scanning engine registry..."

    $engineDirs = @(
        "src/engines/T0U",
        "src/engines/T1M",
        "src/engines/T2E",
        "src/engines/T3I",
        "src/engines/T4A"
    )

    $totalEngines = 0
    foreach ($dir in $engineDirs) {
        $fullPath = Join-Path $SolutionRoot $dir
        if (Test-Path $fullPath) {
            $engines = Get-ChildItem -Path $fullPath -Recurse -Filter "*Engine.cs" |
                Where-Object { $_.Name -notmatch "Test|Mock|Stub" }
            if ($EngineFilter) {
                $engines = $engines | Where-Object { $_.Name -like "*$EngineFilter*" }
            }
            $count = ($engines | Measure-Object).Count
            $totalEngines += $count
            $tier = Split-Path $dir -Leaf
            Write-Host "  $tier : $count engines" -ForegroundColor White
            if ($Verbose) {
                foreach ($e in $engines) {
                    Write-Host "    - $($e.Name)" -ForegroundColor DarkGray
                }
            }
        }
    }
    Write-Host "  Total: $totalEngines engines" -ForegroundColor Green
    Write-Host ""
}

# --- Workflow Definitions ---
if ($Target -eq "workflows" -or $Target -eq "all") {
    Write-Step 2 3 "Scanning workflow definitions..."

    $wssPath = Join-Path $SolutionRoot "src/systems/midstream/WSS"
    if (Test-Path $wssPath) {
        $workflows = Get-ChildItem -Path $wssPath -Recurse -Filter "*Workflow.cs" |
            Where-Object { $_.Name -notmatch "Test|Mock" }
        $count = ($workflows | Measure-Object).Count
        Write-Host "  WSS workflow definitions: $count" -ForegroundColor White
        if ($Verbose) {
            foreach ($w in $workflows) {
                Write-Host "    - $($w.Name)" -ForegroundColor DarkGray
            }
        }
    } else {
        Write-Host "  WSS path not found" -ForegroundColor DarkGray
    }
    Write-Host ""
}

# --- Partition Configuration ---
if ($Target -eq "partitions" -or $Target -eq "all") {
    Write-Step 3 3 "Scanning partition configuration..."

    $partitionPath = Join-Path $SolutionRoot "src/runtime/partition"
    if (Test-Path $partitionPath) {
        $files = Get-ChildItem -Path $partitionPath -Recurse -Filter "*.cs"
        $count = ($files | Measure-Object).Count
        Write-Host "  Partition runtime files: $count" -ForegroundColor White
    } else {
        Write-Host "  Partition path not found" -ForegroundColor DarkGray
    }
    Write-Host ""
}

Write-Success "Runtime inspection complete."
