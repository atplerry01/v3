#!/usr/bin/env bash
# Whycespace Local Development Startup (Bash)
set -euo pipefail

INFRA_ONLY="${1:-false}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/../../infrastructure/localdev/docker-compose.yml"

echo "========================================"
echo " Whycespace Local Dev Environment"
echo "========================================"
echo ""

echo "Starting infrastructure services..."
docker compose -f "$COMPOSE_FILE" up -d

echo ""
echo "Waiting for services to be healthy..."
sleep 10

echo ""
echo "Services:"
echo "  Kafka:      localhost:29092"
echo "  Postgres:   localhost:5432  (whyce/whyce_dev)"
echo "  Redis:      localhost:6379"
echo "  Prometheus: http://localhost:9090"
echo "  Grafana:    http://localhost:3000 (admin/whyce_dev)"
echo ""

if [ "$INFRA_ONLY" != "true" ]; then
    echo "Starting Whycespace Platform..."
    PLATFORM_PROJECT="$SCRIPT_DIR/../../src/platform/Whycespace.Platform.csproj"
    dotnet run --project "$PLATFORM_PROJECT"
fi
