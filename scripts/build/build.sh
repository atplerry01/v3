#!/usr/bin/env bash
# Whycespace Build Script (Bash)
set -euo pipefail

CONFIGURATION="${1:-Release}"
SKIP_TESTS="${2:-false}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_PATH="$SCRIPT_DIR/../../Whycespace.slnx"

echo "========================================"
echo " Whycespace WBSM v3 Build"
echo "========================================"
echo ""

echo "[1/4] Restoring dependencies..."
dotnet restore "$SOLUTION_PATH"

echo "[2/4] Building solution ($CONFIGURATION)..."
dotnet build "$SOLUTION_PATH" --no-restore --configuration "$CONFIGURATION"

if [ "$SKIP_TESTS" != "true" ]; then
    echo "[3/4] Running tests..."
    dotnet test "$SOLUTION_PATH" --no-build --configuration "$CONFIGURATION" \
        --logger "trx;LogFileName=test-results.trx" \
        --results-directory ./test-results
else
    echo "[3/4] Skipping tests"
fi

echo "[4/4] Publishing platform..."
PLATFORM_PROJECT="$SCRIPT_DIR/../../src/platform/Whycespace.Platform.csproj"
dotnet publish "$PLATFORM_PROJECT" --no-build --configuration "$CONFIGURATION" --output ./artifacts/platform

echo ""
echo "========================================"
echo " Build succeeded"
echo "========================================"
