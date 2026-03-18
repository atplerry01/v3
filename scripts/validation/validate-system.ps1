# Whycespace System Validator
# Validates architecture compliance, layer boundaries, and structural integrity.
param(
    [ValidateSet("layers", "references", "contracts", "naming", "all")]
    [string]$Check = "all",

    [switch]$Strict,

    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
Import-Module "$PSScriptRoot/../shared/ScriptHelpers.psm1"

Write-Banner "Whycespace System Validator"

$SolutionRoot = Get-SolutionRoot
$errors = @()
$warnings = @()

# --- Layer Boundary Validation ---
if ($Check -eq "layers" -or $Check -eq "all") {
    Write-Step 1 4 "Validating layer boundaries..."

    # Domain must not reference runtime, platform, or infrastructure
    $domainPath = Join-Path $SolutionRoot "src/domain"
    if (Test-Path $domainPath) {
        $domainFiles = Get-ChildItem -Path $domainPath -Recurse -Filter "*.cs"
        foreach ($f in $domainFiles) {
            $content = Get-Content $f.FullName -Raw
            if ($content -match "using Whycespace\.(Runtime|Platform|Infrastructure)") {
                $rel = $f.FullName -replace [regex]::Escape($SolutionRoot), ''
                $errors += "Layer violation: $rel references Runtime/Platform/Infrastructure"
            }
        }
    }

    # Engines must not reference platform or infrastructure
    $enginesPath = Join-Path $SolutionRoot "src/engines"
    if (Test-Path $enginesPath) {
        $engineFiles = Get-ChildItem -Path $enginesPath -Recurse -Filter "*.cs"
        foreach ($f in $engineFiles) {
            $content = Get-Content $f.FullName -Raw
            if ($content -match "using Whycespace\.(Platform|Infrastructure)") {
                $rel = $f.FullName -replace [regex]::Escape($SolutionRoot), ''
                $errors += "Layer violation: $rel references Platform/Infrastructure"
            }
        }
    }

    $layerViolations = ($errors | Measure-Object).Count
    if ($layerViolations -eq 0) {
        Write-Success "  Layer boundaries: OK"
    } else {
        Write-Failure "  Layer violations found: $layerViolations"
    }
    Write-Host ""
}

# --- Project Reference Validation ---
if ($Check -eq "references" -or $Check -eq "all") {
    Write-Step 2 4 "Validating project references..."

    $csprojFiles = Get-ChildItem -Path (Join-Path $SolutionRoot "src") -Recurse -Filter "*.csproj"
    $missingRefs = 0
    foreach ($proj in $csprojFiles) {
        $content = Get-Content $proj.FullName -Raw
        $refs = [regex]::Matches($content, 'Include="([^"]+\.csproj)"')
        foreach ($ref in $refs) {
            $refPath = Join-Path $proj.DirectoryName $ref.Groups[1].Value
            $resolved = [System.IO.Path]::GetFullPath($refPath)
            if (-not (Test-Path $resolved)) {
                $rel = $proj.FullName -replace [regex]::Escape($SolutionRoot), ''
                $errors += "Broken reference in $rel : $($ref.Groups[1].Value)"
                $missingRefs++
            }
        }
    }

    if ($missingRefs -eq 0) {
        Write-Success "  Project references: OK ($(($csprojFiles | Measure-Object).Count) projects scanned)"
    } else {
        Write-Failure "  Broken references: $missingRefs"
    }
    Write-Host ""
}

# --- Contract Consistency ---
if ($Check -eq "contracts" -or $Check -eq "all") {
    Write-Step 3 4 "Validating contract consistency..."

    $contractsPath = Join-Path $SolutionRoot "src/shared/contracts"
    if (Test-Path $contractsPath) {
        $interfaces = Get-ChildItem -Path $contractsPath -Recurse -Filter "I*.cs"
        $count = ($interfaces | Measure-Object).Count
        Write-Host "  Shared contract interfaces: $count" -ForegroundColor White
    }

    $primitivesPath = Join-Path $SolutionRoot "src/shared/primitives"
    if (Test-Path $primitivesPath) {
        $primitives = Get-ChildItem -Path $primitivesPath -Recurse -Filter "*.cs"
        $count = ($primitives | Measure-Object).Count
        Write-Host "  Shared primitives: $count" -ForegroundColor White
    }
    Write-Host ""
}

# --- Naming Convention Check ---
if ($Check -eq "naming" -or $Check -eq "all") {
    Write-Step 4 4 "Validating naming conventions..."

    $enginesPath = Join-Path $SolutionRoot "src/engines"
    if (Test-Path $enginesPath) {
        $engineFiles = Get-ChildItem -Path $enginesPath -Recurse -Filter "*.cs" |
            Where-Object { $_.Name -match "Engine\.cs$" }
        $nonConforming = $engineFiles | Where-Object { $_.Name -notmatch "^[A-Z][a-zA-Z]+Engine\.cs$" }
        if (($nonConforming | Measure-Object).Count -gt 0) {
            foreach ($nc in $nonConforming) {
                $warnings += "Non-standard engine name: $($nc.Name)"
            }
        }
        Write-Host "  Engine files checked: $(($engineFiles | Measure-Object).Count)" -ForegroundColor White
    }
    Write-Host ""
}

# --- Summary ---
Write-Host ""
$totalErrors = ($errors | Measure-Object).Count
$totalWarnings = ($warnings | Measure-Object).Count

if ($Verbose -or $totalErrors -gt 0) {
    foreach ($e in $errors) { Write-Failure "  ERROR: $e" }
}
if ($Verbose -or $totalWarnings -gt 0) {
    foreach ($w in $warnings) { Write-Host "  WARN: $w" -ForegroundColor DarkYellow }
}

Write-Host ""
if ($totalErrors -eq 0) {
    Write-Success "Validation passed. Errors: 0, Warnings: $totalWarnings"
} else {
    Write-Failure "Validation failed. Errors: $totalErrors, Warnings: $totalWarnings"
    if ($Strict) { exit 1 }
}
