# WHYCESPACE WBSM v3
# PHASE 1.9.5 — OBSERVABILITY

You are implementing **Phase 1.9.5 of the Whycespace system**.

This phase introduces **Observability Infrastructure**.

Observability allows operators and developers to understand system behavior in real time.

This includes:

• runtime metrics  
• distributed tracing  
• workflow diagnostics  
• engine telemetry  
• system health monitoring  

---

# OBJECTIVES

1 Implement Metrics Collector  
2 Implement Distributed Trace System  
3 Implement Workflow Diagnostics  
4 Implement Engine Execution Telemetry  
5 Implement Health Monitoring  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/observability/
```

Structure:

```
src/runtime/observability/

├── metrics/
├── tracing/
├── diagnostics/
├── health/
└── models/
```

Create project:

```
Whycespace.Observability.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.RuntimeDispatcher
Whycespace.EventFabric
```

---

# METRICS COLLECTOR

Folder:

```
metrics/
```

Create:

```
MetricsCollector.cs
```

Purpose:

Collect runtime metrics.

Metrics examples:

```
WorkflowExecutions
EngineInvocations
EventsPublished
RetryAttempts
```

Example structure:

```csharp
public sealed class MetricsCollector
{
    private readonly Dictionary<string,long> _metrics;

    public void Increment(string metric)
    {
        if (!_metrics.ContainsKey(metric))
            _metrics[metric] = 0;

        _metrics[metric]++;
    }

    public long Get(string metric)
    {
        return _metrics.TryGetValue(metric, out var value)
            ? value
            : 0;
    }
}
```

---

# DISTRIBUTED TRACING

Folder:

```
tracing/
```

Create:

```
TraceContext.cs
```

Purpose:

Track request flow across system components.

Fields:

```
TraceId
SpanId
ParentSpanId
Timestamp
```

Example:

```csharp
public sealed record TraceContext(
    Guid TraceId,
    Guid SpanId,
    Guid? ParentSpanId,
    DateTime Timestamp
);
```

---

Create:

```
TraceManager.cs
```

Responsibilities:

```
start trace
create spans
track execution flow
```

---

# WORKFLOW DIAGNOSTICS

Folder:

```
diagnostics/
```

Create:

```
WorkflowDiagnosticsService.cs
```

Purpose:

Track workflow execution state.

Responsibilities:

```
track workflow start
track workflow step execution
track workflow completion
record failures
```

Example methods:

```
RecordWorkflowStart
RecordStepExecution
RecordWorkflowFailure
```

---

# ENGINE TELEMETRY

Folder:

```
diagnostics/
```

Create:

```
EngineTelemetryService.cs
```

Purpose:

Track engine performance.

Metrics collected:

```
ExecutionTime
SuccessCount
FailureCount
```

Example methods:

```
RecordEngineStart
RecordEngineCompletion
```

---

# HEALTH MONITORING

Folder:

```
health/
```

Create:

```
HealthCheckService.cs
```

Purpose:

Monitor health of system components.

Components checked:

```
Runtime Dispatcher
Kafka Event Fabric
Partition Workers
Engine Worker Pools
```

Example:

```csharp
public sealed class HealthCheckService
{
    public bool CheckRuntimeHealth()
}
```

---

# INTEGRATION WITH RUNTIME

Modify:

```
RuntimeDispatcher
WorkflowExecutor
EngineWorker
```

Add instrumentation.

Example flow:

```
WorkflowExecution
 ↓
TraceManager.StartTrace
 ↓
WorkflowDiagnosticsService.RecordStart
 ↓
EngineTelemetryService.RecordEngineExecution
 ↓
MetricsCollector.Increment("WorkflowExecutions")
```

---

# SAMPLE TRACE FLOW

Taxi Ride Request

```
API Request
 ↓
Trace Start
 ↓
Workflow Runtime
 ↓
DriverMatchingEngine
 ↓
RideCreationEngine
 ↓
Trace Complete
```

TraceId remains constant across all steps.

---

# UNIT TESTS

Create project:

```
tests/observability/
```

Tests:

```
MetricsCollectorTests.cs
TraceManagerTests.cs
WorkflowDiagnosticsTests.cs
HealthCheckTests.cs
```

Test cases:

```
metrics increment
trace creation
workflow diagnostics recording
health monitoring
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/metrics
```

Return system metrics.

Example:

```json
{
  "workflowExecutions": 100,
  "engineInvocations": 350
}
```

---

GET

```
/dev/traces
```

Return active traces.

Example:

```json
{
  "activeTraces": 5
}
```

---

GET

```
/dev/health
```

Return system health.

Example:

```json
{
  "runtime": "healthy",
  "eventFabric": "healthy",
  "workers": "healthy"
}
```

---

# BUILD VALIDATION

Run:

```
dotnet build
```

Expected:

```
Build succeeded
0 warnings
0 errors
```

---

# TEST VALIDATION

Run:

```
dotnet test
```

Expected:

```
Tests:
4 passed
0 failed
```

---

# OUTPUT FORMAT

Return:

```
1 Files Created
2 Repository Tree
3 Build Result
4 Test Result
5 Debug Endpoints
```

Example:

```
Build succeeded
0 warnings
0 errors

Tests:
4 passed
0 failed
```

---

# PHASE COMPLETION CRITERIA

Phase 1.9.5 is complete when:

• metrics collection works  
• distributed traces track execution  
• workflow diagnostics capture runtime behavior  
• engine telemetry records performance  
• health monitoring detects failures  
• tests pass  
• debug endpoints respond  

End of Phase 1.9.5.