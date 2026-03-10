# Whycespace WBSM v3 — Load Simulation Framework

## Overview

The Load Simulation Framework stress-tests the Whycespace runtime architecture by generating high volumes of workflows and measuring system performance across all layers: command dispatch, engine execution, event publishing, and projection updates.

## Architecture

```
┌─────────────┐     ┌──────────────────┐     ┌─────────────────┐
│ CLI Parser   │────▶│ SimulationRunner │────▶│ WorkflowPayloads│
└─────────────┘     │                  │     └────────┬────────┘
                    │  SemaphoreSlim   │              │
                    │  (N workers)     │              ▼
                    │                  │     ┌─────────────────┐
                    │  FaultInjector   │────▶│ RuntimeDispatch │
                    │                  │     │ EngineRegistry  │
                    └──────┬───────────┘     │ EngineExecution │
                           │                 └────────┬────────┘
                           ▼                          │
                    ┌──────────────────┐              ▼
                    │ SimulationMetrics │     ┌─────────────────┐
                    │  (concurrent)    │◀────│ Events/Latency  │
                    └──────┬───────────┘     └─────────────────┘
                           │
                           ▼
                    ┌──────────────────┐
                    │ SimulationReport │──▶ Console + JSON
                    └──────────────────┘
```

### Components

| Component | Responsibility |
|-----------|---------------|
| `SimulationScenario` | Defines Small (1K), Medium (50K), Large (1M) presets |
| `WorkloadGenerator` | Randomly generates RideRequest, PropertyListing, EconomicLifecycle payloads |
| `SimulationRunner` | Orchestrates concurrent workflow execution via SemaphoreSlim |
| `SimulationMetrics` | Thread-safe counters, latency tracking, percentile computation |
| `SimulationReport` | Formats results for console and JSON output |
| `FaultInjector` | Randomly fails engine invocations at a configurable rate |
| `FaultAwareDispatcher` | Wraps RuntimeDispatcher with retry and dead-letter support |

## How to Run

```bash
cd simulation/Whycespace.Simulation

# Small load (1,000 workflows, 10 workers)
dotnet run -- --scenario small

# Medium load with 100 workers
dotnet run -- --scenario medium --workers 100

# Large load with 1000 workers and JSON output
dotnet run -- --scenario large --workers 1000 --output report.json

# Custom count with fault injection
dotnet run -- --scenario 5000 --workers 200 --fault-rate 0.05

# Time-limited run
dotnet run -- --scenario large --workers 500 --duration 60
```

### CLI Options

| Option | Alias | Default | Description |
|--------|-------|---------|-------------|
| `--scenario` | `-s` | `small` | `small`, `medium`, `large`, or a number |
| `--workers` | `-w` | `10` | Concurrent worker count |
| `--duration` | `-d` | unlimited | Max duration in seconds |
| `--fault-rate` | `-f` | `0` | Fault injection rate (0.0 to 1.0) |
| `--output` | `-o` | none | JSON report output path |
| `--help` | `-h` | — | Show help |

## Scenarios

| Scenario | Workflows | Recommended Workers | Purpose |
|----------|-----------|-------------------|---------|
| Small | 1,000 | 10–50 | Smoke test, CI validation |
| Medium | 50,000 | 100–500 | Performance baseline |
| Large | 1,000,000 | 500–1000 | Capacity planning, stress test |

## Interpreting Results

### Throughput

- **Workflows/sec**: End-to-end workflow execution rate. Target: >10,000/sec for in-memory.
- **Engine invocations/sec**: Raw engine dispatch rate. Each workflow has 4–6 steps.
- **Events published/sec**: Event production rate tracking Kafka readiness.

### Latency

- **Average**: Mean workflow execution time. Sub-1ms expected for in-memory.
- **P95**: 95th percentile — most users experience this or better.
- **P99**: 99th percentile — tail latency indicator.
- **Max**: Worst-case latency. GC pauses or thread contention.

### Reliability

- **Success rate**: Should be 100% without fault injection.
- **Dead lettered**: Count of permanently failed invocations.
- **Retries attempted**: Indicates transient failure handling.

### Fault Injection

Set `--fault-rate 0.05` to inject 5% random engine failures. This validates:

- `RetryPolicyEngine` exponential backoff behavior
- `DeadLetterQueue` captures unrecoverable failures
- Overall system degradation under partial failure

Expected behavior with 5% fault rate: ~80–90% success rate (faults cascade through multi-step workflows).

## JSON Report Schema

```json
{
  "Scenario": { "Name": "Small", "TotalWorkflows": 1000, "Workers": 50, "FaultRate": 0 },
  "Execution": { "TotalExecuted": 1000, "TotalSucceeded": 1000, "SuccessRate": 100, "WallClockSeconds": 0.04 },
  "Throughput": { "WorkflowsPerSecond": 27322, "EngineInvocationsPerSecond": 126174, "EventsPerSecond": 126174 },
  "WorkflowLatency": { "AverageMs": 0.09, "P95Ms": 0.03, "P99Ms": 0.19, "MaxMs": 9.40 },
  "EngineLatency": { "AverageMs": 0.0085 },
  "ProjectionLatency": { "AverageMs": 0.0 },
  "Reliability": { "TotalEngineInvocations": 4618, "TotalEventsPublished": 4618, "TotalRetries": 0, "TotalDeadLettered": 0 },
  "WorkflowDistribution": { "RideRequest": 309, "PropertyListing": 351, "EconomicLifecycle": 340 },
  "GeneratedAt": "2026-03-10T..."
}
```
